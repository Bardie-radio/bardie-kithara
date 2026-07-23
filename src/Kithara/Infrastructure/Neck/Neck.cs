using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Bardie.Orchestrator.Source;
using Bardie.Source.V1;
using Kithara.Infrastructure.Persistence;
using Kithara.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kithara.Infrastructure.Neck;

/// <summary>
/// Stream lifecycle (Neck): alive Strunas, session FIFOs, encode supervisor, source track jobs,
/// TrackStatus → now-playing / queue advance.
/// </summary>
public sealed class Neck
{
    // 0666 — shared Compose volume; modules + Kithara both open the pipe.
    private const uint FifoMode = 0x1B6;

    private readonly string _fifoRoot;
    private readonly IDbContextFactory<KitharaDbContext> _dbFactory;
    private readonly SourceModuleOrchestrator _orch;
    private readonly StrunaEncoderSupervisor _encoder;
    private readonly ConcurrentDictionary<Guid, ActiveTrackJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, NowPlayingSnapshot> _nowPlaying = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _statusWatchers = new();
    private readonly ILogger<Neck> _logger;

    public Neck(
        IOptions<NeckOptions> options,
        IDbContextFactory<KitharaDbContext> dbFactory,
        SourceModuleOrchestrator orch,
        StrunaEncoderSupervisor encoder,
        ILogger<Neck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _orch = orch ?? throw new ArgumentNullException(nameof(orch));
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var path = options.Value.StrunaFifoRoot;
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _fifoRoot = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.Combine(_fifoRoot, "strunas"));
    }

    /// <summary>Absolute path of the Struna FIFO without creating the node.</summary>
    public string GetStrunaFifoPath(Guid strunaId)
    {
        if (strunaId == Guid.Empty)
        {
            throw new ArgumentException("Struna id is required.", nameof(strunaId));
        }

        return Path.Combine(_fifoRoot, "strunas", $"{strunaId:D}.pcm");
    }

    public async Task<IReadOnlyList<Struna>> ListStrunasAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Strunas.AsNoTracking()
            .Include(s => s.ControlGrants)
            .OrderBy(s => s.Slug)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Struna?> GetStrunaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Strunas.AsNoTracking()
            .Include(s => s.ControlGrants)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Struna?> GetStrunaBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSlug(slug);
        if (normalized is null)
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Strunas.AsNoTracking()
            .Include(s => s.ControlGrants)
            .FirstOrDefaultAsync(s => s.Slug == normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an encode-alive Struna: persist row + session FIFO + silence + FFmpeg.
    /// </summary>
    public async Task<CreateStrunaOutcome> CreateStrunaAsync(
        Guid ownerUserId,
        string slug,
        string? title,
        PlaybackAccess playback,
        ControlAccess control,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeSlug(slug);
        if (normalized is null)
        {
            return new CreateStrunaOutcome(null, CreateStrunaError.InvalidSlug);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (await db.Strunas.AnyAsync(s => s.Slug == normalized, cancellationToken).ConfigureAwait(false))
        {
            return new CreateStrunaOutcome(null, CreateStrunaError.SlugConflict);
        }

        var struna = new Struna
        {
            Id = Guid.NewGuid(),
            Slug = normalized,
            Title = string.IsNullOrWhiteSpace(title) ? normalized : title.Trim(),
            PlaybackAccess = playback,
            ControlAccess = control,
            OwnerUserId = ownerUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            GuestCode = CreateSecret(6),
            ListenToken = playback == PlaybackAccess.Protected ? CreateSecret(24) : null,
        };

        db.Strunas.Add(struna);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            return new CreateStrunaOutcome(null, CreateStrunaError.SlugConflict);
        }

        using var activity = NeckActivity.Source.StartActivity("neck.struna.create");
        activity?.SetTag("struna.id", struna.Id.ToString("D"));
        activity?.SetTag("struna.slug", struna.Slug);

        var fifo = await EnsureStrunaFifoAsync(struna.Id, cancellationToken).ConfigureAwait(false);
        try
        {
            await _encoder.StartAsync(struna.Id, struna.Slug, fifo, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start encoder for Struna {Id}; rolling back", struna.Id);
            await RemoveStrunaFifoAsync(struna.Id, cancellationToken).ConfigureAwait(false);
            db.Strunas.Remove(struna);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }

        return new CreateStrunaOutcome(struna, null);
    }

    /// <summary>
    /// Tears down a Struna: StopTrack → silence/FFmpeg stop → remove FIFO → destroy guests → free slug.
    /// Returns destroyed guest user ids so Search can clear their result <b>cache</b> (not search history).
    /// </summary>
    public async Task<DeleteStrunaOutcome> DeleteStrunaAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var struna = await db.Strunas.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (struna is null)
        {
            return new DeleteStrunaOutcome(false, []);
        }

        using var activity = NeckActivity.Source.StartActivity("neck.struna.delete");
        activity?.SetTag("struna.id", id.ToString("D"));
        activity?.SetTag("struna.slug", struna.Slug);

        await StopCurrentTrackAsync(id, cancellationToken).ConfigureAwait(false);
        ClearNowPlaying(id);
        await _encoder.StopAsync(id, cancellationToken).ConfigureAwait(false);
        await RemoveStrunaFifoAsync(id, cancellationToken).ConfigureAwait(false);

        var guests = await db.Users
            .Where(u => u.GuestStrunaId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var guestIdList = guests.Select(u => u.Id).ToArray();
        if (guests.Count > 0)
        {
            db.Users.RemoveRange(guests);
        }

        db.Strunas.Remove(struna);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new DeleteStrunaOutcome(true, guestIdList);
    }

    /// <summary>
    /// Starts (or replaces) a track job. Empty module/trackRef = unpause: silence off + ResumeTrack.
    /// Encoder stays up across play / skip.
    /// </summary>
    public async Task<PlayTrackOutcome> PlayTrackAsync(
        Guid strunaId,
        string? moduleSlug,
        string? trackRef,
        CancellationToken cancellationToken = default)
    {
        var struna = await GetStrunaAsync(strunaId, cancellationToken).ConfigureAwait(false);
        if (struna is null)
        {
            return new PlayTrackOutcome(false, null, PlayTrackError.StrunaNotFound, null);
        }

        if (string.IsNullOrWhiteSpace(moduleSlug) || string.IsNullOrWhiteSpace(trackRef))
        {
            return await UnpauseAsync(strunaId, cancellationToken).ConfigureAwait(false);
        }

        await StopCurrentTrackAsync(strunaId, cancellationToken).ConfigureAwait(false);
        // Keep encoder; silence fills the gap until the module writer attaches.
        _encoder.SetSilence(strunaId, true);

        var fifo = await EnsureStrunaFifoAsync(strunaId, cancellationToken).ConfigureAwait(false);
        var start = await _orch.StartTrackAsync(
                moduleSlug.Trim(),
                strunaId.ToString("D"),
                trackRef.Trim(),
                fifo,
                cancellationToken)
            .ConfigureAwait(false);

        if (!start.Ok || string.IsNullOrWhiteSpace(start.TrackJobId))
        {
            return new PlayTrackOutcome(
                false,
                null,
                PlayTrackError.ModuleFailed,
                start.FailureReason ?? "start_track_failed");
        }

        var job = new ActiveTrackJob(start.ModuleSlug!, start.TrackJobId, trackRef.Trim());
        _jobs[strunaId] = job;
        SetNowPlaying(strunaId, new NowPlayingSnapshot(
            job.ModuleSlug,
            job.TrackRef,
            job.TrackJobId,
            Title: null,
            Artist: null,
            Paused: false));
        // Module owns PCM; silence off so Magpie bytes are not interleaved with zeros.
        _encoder.SetSilence(strunaId, false);
        StartStatusWatcher(strunaId, job);
        return new PlayTrackOutcome(true, start.TrackJobId, null, null);
    }

    /// <summary>
    /// Pause: silence on + optional module <c>PauseTrack</c> when the source advertises pause.
    /// </summary>
    public async Task<PlayTrackOutcome> PauseTrackAsync(
        Guid strunaId,
        CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(strunaId, out var job))
        {
            // Still feed silence so FFmpeg keeps humming for an idle Struna.
            _encoder.SetSilence(strunaId, true);
            return new PlayTrackOutcome(false, null, PlayTrackError.NothingToResume, "no_active_track");
        }

        _encoder.SetSilence(strunaId, true);

        var pause = await _orch.PauseTrackAsync(job.ModuleSlug, job.TrackJobId, cancellationToken)
            .ConfigureAwait(false);
        if (!pause.Ok)
        {
            // Module may lack pause — silence still holds the encoder (plan: optional PauseTrack).
            _logger.LogInformation(
                "PauseTrack unavailable or failed for Struna {Id}: {Reason}; silence feeder active",
                strunaId,
                pause.FailureReason);
        }

        if (_nowPlaying.TryGetValue(strunaId, out var snap))
        {
            SetNowPlaying(strunaId, snap with { Paused = true });
        }

        return new PlayTrackOutcome(true, job.TrackJobId, null, null);
    }

    /// <summary>Stop current job and start the next queue entry, if any. Never restarts FFmpeg.</summary>
    public async Task<PlayTrackOutcome> SkipAsync(Guid strunaId, CancellationToken cancellationToken = default)
    {
        await StopCurrentTrackAsync(strunaId, cancellationToken).ConfigureAwait(false);
        _encoder.SetSilence(strunaId, true);

        return await AdvanceQueueHeadAsync(strunaId, cancellationToken).ConfigureAwait(false);
    }

    public NowPlayingInfo? GetNowPlaying(Guid strunaId)
    {
        if (_nowPlaying.TryGetValue(strunaId, out var snap))
        {
            return snap.ToInfo();
        }

        return _jobs.TryGetValue(strunaId, out var job)
            ? new NowPlayingInfo(job.ModuleSlug, job.TrackRef, job.TrackJobId, null, null, false)
            : null;
    }

    /// <summary>ICY <c>StreamTitle</c> text from the Neck snapshot (same source as REST now-playing).</summary>
    public string GetStreamTitle(Guid strunaId)
    {
        var now = GetNowPlaying(strunaId);
        if (now is null || now.Paused)
        {
            return string.Empty;
        }

        return now.StreamTitle;
    }

    public async Task<IReadOnlyList<QueueEntry>> ListQueueAsync(
        Guid strunaId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.QueueEntries.AsNoTracking()
            .Include(e => e.Tune)
            .Where(e => e.StrunaId == strunaId)
            .OrderBy(e => e.Position)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(QueueEntry? Entry, string? Error)> EnqueueTuneAsync(
        Guid strunaId,
        Guid tuneId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        if (!await db.Strunas.AnyAsync(s => s.Id == strunaId, cancellationToken).ConfigureAwait(false))
        {
            return (null, "struna_not_found");
        }

        if (!await db.Tunes.AnyAsync(t => t.Id == tuneId, cancellationToken).ConfigureAwait(false))
        {
            return (null, "tune_not_found");
        }

        var maxPos = await db.QueueEntries
            .Where(e => e.StrunaId == strunaId)
            .Select(e => (int?)e.Position)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) ?? -1;

        var entry = new QueueEntry
        {
            Id = Guid.NewGuid(),
            StrunaId = strunaId,
            TuneId = tuneId,
            Position = maxPos + 1,
        };
        db.QueueEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await db.Entry(entry).Reference(e => e.Tune).LoadAsync(cancellationToken).ConfigureAwait(false);
        return (entry, null);
    }

    public async Task<bool> RemoveQueueEntryAsync(
        Guid strunaId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var entry = await db.QueueEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.StrunaId == strunaId, cancellationToken)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return false;
        }

        db.QueueEntries.Remove(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public bool TryGetActiveTrack(Guid strunaId, out ActiveTrackJob? job) =>
        _jobs.TryGetValue(strunaId, out job);

    private async Task<PlayTrackOutcome> UnpauseAsync(Guid strunaId, CancellationToken cancellationToken)
    {
        if (!_jobs.TryGetValue(strunaId, out var existing))
        {
            return new PlayTrackOutcome(false, null, PlayTrackError.NothingToResume, null);
        }

        _encoder.SetSilence(strunaId, false);

        var resume = await _orch.ResumeTrackAsync(
                existing.ModuleSlug,
                existing.TrackJobId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!resume.Ok)
        {
            _logger.LogInformation(
                "ResumeTrack unavailable or failed for Struna {Id}: {Reason}; silence off for module writer",
                strunaId,
                resume.FailureReason);
        }

        if (_nowPlaying.TryGetValue(strunaId, out var snap))
        {
            SetNowPlaying(strunaId, snap with { Paused = false });
        }

        return new PlayTrackOutcome(true, existing.TrackJobId, null, null);
    }

    private async Task<PlayTrackOutcome> AdvanceQueueHeadAsync(
        Guid strunaId,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var next = await db.QueueEntries
            .Include(e => e.Tune)
            .Where(e => e.StrunaId == strunaId)
            .OrderBy(e => e.Position)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is null)
        {
            ClearNowPlaying(strunaId);
            return new PlayTrackOutcome(true, null, null, null);
        }

        db.QueueEntries.Remove(next);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var trackRef = string.IsNullOrWhiteSpace(next.Tune.ExternalId)
            ? next.Tune.Id.ToString("D")
            : next.Tune.ExternalId;
        var outcome = await PlayTrackAsync(strunaId, next.Tune.ModuleSlug, trackRef, cancellationToken)
            .ConfigureAwait(false);

        if (outcome.Ok
            && (!string.IsNullOrWhiteSpace(next.Tune.Title) || !string.IsNullOrWhiteSpace(next.Tune.Artist)))
        {
            if (_nowPlaying.TryGetValue(strunaId, out var snap))
            {
                SetNowPlaying(
                    strunaId,
                    snap with { Title = next.Tune.Title, Artist = next.Tune.Artist });
            }
        }

        return outcome;
    }

    private void StartStatusWatcher(Guid strunaId, ActiveTrackJob job)
    {
        StopStatusWatcher(strunaId);
        var cts = new CancellationTokenSource();
        _statusWatchers[strunaId] = cts;
        _ = Task.Run(() => WatchTrackStatusAsync(strunaId, job, cts.Token), CancellationToken.None);
    }

    private void StopStatusWatcher(Guid strunaId)
    {
        if (_statusWatchers.TryRemove(strunaId, out var cts))
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed.
            }

            cts.Dispose();
        }
    }

    private async Task WatchTrackStatusAsync(
        Guid strunaId,
        ActiveTrackJob job,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? ownedCts = null;
        try
        {
            await foreach (var evt in _orch.TrackStatusAsync(job.ModuleSlug, job.TrackJobId, cancellationToken)
                               .ConfigureAwait(false))
            {
                if (!_jobs.TryGetValue(strunaId, out var current)
                    || !string.Equals(current.TrackJobId, job.TrackJobId, StringComparison.Ordinal))
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(evt.Title) || !string.IsNullOrWhiteSpace(evt.Artist))
                {
                    var prev = _nowPlaying.TryGetValue(strunaId, out var snap)
                        ? snap
                        : new NowPlayingSnapshot(
                            job.ModuleSlug,
                            job.TrackRef,
                            job.TrackJobId,
                            null,
                            null,
                            false);
                    SetNowPlaying(
                        strunaId,
                        prev with
                        {
                            Title = string.IsNullOrWhiteSpace(evt.Title) ? prev.Title : evt.Title,
                            Artist = string.IsNullOrWhiteSpace(evt.Artist) ? prev.Artist : evt.Artist,
                            Paused = evt.State == TrackState.Paused,
                        });
                }
                else if (evt.State == TrackState.Paused
                         && _nowPlaying.TryGetValue(strunaId, out var pausedSnap))
                {
                    SetNowPlaying(strunaId, pausedSnap with { Paused = true });
                }
                else if (evt.State == TrackState.Running
                         && _nowPlaying.TryGetValue(strunaId, out var runSnap))
                {
                    SetNowPlaying(strunaId, runSnap with { Paused = false });
                }

                if (evt.State is TrackState.Ended or TrackState.Error)
                {
                    if (evt.State == TrackState.Error)
                    {
                        _logger.LogWarning(
                            "Track job {JobId} on Struna {Id} errored: {Error}",
                            job.TrackJobId,
                            strunaId,
                            evt.ErrorMessage);
                    }

                    // DES-02: clear job + advance queue; encoder stays up.
                    if (_jobs.TryGetValue(strunaId, out var still)
                        && string.Equals(still.TrackJobId, job.TrackJobId, StringComparison.Ordinal))
                    {
                        _jobs.TryRemove(strunaId, out _);
                        // Detach before AdvanceQueueHead starts a new watcher for the next track.
                        if (_statusWatchers.TryRemove(strunaId, out var removed))
                        {
                            if (removed.Token.Equals(cancellationToken))
                            {
                                ownedCts = removed;
                            }
                            else
                            {
                                removed.Cancel();
                                removed.Dispose();
                            }
                        }

                        _encoder.SetSilence(strunaId, true);
                        try
                        {
                            await AdvanceQueueHeadAsync(strunaId, CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Queue advance failed after track end on Struna {Id}", strunaId);
                            ClearNowPlaying(strunaId);
                        }
                    }

                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Watcher cancelled (stop / delete / replace).
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "TrackStatus watcher ended for Struna {Id} job {JobId}",
                strunaId,
                job.TrackJobId);
        }
        finally
        {
            ownedCts?.Dispose();
        }
    }

    private void SetNowPlaying(Guid strunaId, NowPlayingSnapshot snapshot)
    {
        _nowPlaying[strunaId] = snapshot;
        _encoder.SetStreamTitle(strunaId, snapshot.ToInfo().StreamTitle);
    }

    private void ClearNowPlaying(Guid strunaId)
    {
        _nowPlaying.TryRemove(strunaId, out _);
        _encoder.SetStreamTitle(strunaId, string.Empty);
    }

    private static string CreateSecret(int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz234567";
        Span<byte> bytes = stackalloc byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }

    /// <summary>Ensures the FIFO exists for an alive Struna; returns the path for <c>audio_endpoint</c>.</summary>
    public Task<string> EnsureStrunaFifoAsync(Guid strunaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetStrunaFifoPath(strunaId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (File.Exists(path))
        {
            return Task.FromResult(path);
        }

        CreateNamedPipe(path);
        _logger.LogInformation("Created Struna FIFO {Path}", path);
        return Task.FromResult(path);
    }

    /// <summary>Removes the Struna FIFO on teardown.</summary>
    public Task RemoveStrunaFifoAsync(Guid strunaId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetStrunaFifoPath(strunaId);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation("Removed Struna FIFO {Path}", path);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to remove Struna FIFO {Path}", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Failed to remove Struna FIFO {Path}", path);
        }

        return Task.CompletedTask;
    }

    private async Task StopCurrentTrackAsync(Guid strunaId, CancellationToken cancellationToken)
    {
        StopStatusWatcher(strunaId);
        if (!_jobs.TryRemove(strunaId, out var job))
        {
            return;
        }

        var stop = await _orch.StopTrackAsync(job.ModuleSlug, job.TrackJobId, cancellationToken)
            .ConfigureAwait(false);
        if (!stop.Ok)
        {
            _logger.LogWarning(
                "StopTrack failed for Struna {Id}: {Reason}",
                strunaId,
                stop.FailureReason);
        }
    }

    private static string? NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var trimmed = slug.Trim().ToLowerInvariant();
        if (trimmed.Length is < 1 or > 64)
        {
            return null;
        }

        foreach (var ch in trimmed)
        {
            if (ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-')
            {
                continue;
            }

            return null;
        }

        return trimmed;
    }

    private static void CreateNamedPipe(string path)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            var rc = MkFifo(path, FifoMode);
            if (rc == 0)
            {
                return;
            }

            var errno = Marshal.GetLastWin32Error();
            // EEXIST — another creator won the race, or leftover node.
            if (errno == 17 && File.Exists(path))
            {
                return;
            }

            throw new IOException($"mkfifo failed for '{path}' (errno {errno}).");
        }

        // Non-Unix hosts (dev/test): regular file placeholder so callers still get a path.
        using var _ = File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "mkfifo")]
    private static extern int MkFifo([MarshalAs(UnmanagedType.LPUTF8Str)] string pathname, uint mode);
}

public sealed record ActiveTrackJob(string ModuleSlug, string TrackJobId, string TrackRef);

public sealed record NowPlayingSnapshot(
    string ModuleSlug,
    string TrackRef,
    string TrackJobId,
    string? Title,
    string? Artist,
    bool Paused)
{
    public NowPlayingInfo ToInfo() =>
        new(ModuleSlug, TrackRef, TrackJobId, Title, Artist, Paused);
}

public sealed record NowPlayingInfo(
    string ModuleSlug,
    string TrackRef,
    string TrackJobId,
    string? Title,
    string? Artist,
    bool Paused)
{
    /// <summary>ICY / REST display title: <c>Artist - Title</c> or trackRef fallback.</summary>
    public string StreamTitle
    {
        get
        {
            var hasArtist = !string.IsNullOrWhiteSpace(Artist);
            var hasTitle = !string.IsNullOrWhiteSpace(Title);
            if (hasArtist && hasTitle)
            {
                return $"{Artist!.Trim()} - {Title!.Trim()}";
            }

            if (hasTitle)
            {
                return Title!.Trim();
            }

            if (hasArtist)
            {
                return Artist!.Trim();
            }

            return TrackRef;
        }
    }
}

public enum CreateStrunaError
{
    InvalidSlug,
    SlugConflict,
}

public sealed record CreateStrunaOutcome(Struna? Struna, CreateStrunaError? Error);

public sealed record DeleteStrunaOutcome(bool Deleted, IReadOnlyList<Guid> GuestUserIds);

public enum PlayTrackError
{
    StrunaNotFound,
    NothingToResume,
    ModuleFailed,
}

public sealed record PlayTrackOutcome(
    bool Ok,
    string? TrackJobId,
    PlayTrackError? Error,
    string? Detail);
