using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Bardie.Orchestrator.Source;
using Kithara.Infrastructure.Persistence;
using Kithara.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kithara.Infrastructure.Neck;

/// <summary>
/// Stream lifecycle (Neck): alive Strunas, session FIFOs, and source track-job control.
/// FFmpeg / silence feeder land in Phase 4 on this type.
/// </summary>
public sealed class Neck
{
    // 0666 — shared Compose volume; modules + Kithara both open the pipe.
    private const uint FifoMode = 0x1B6;

    private readonly string _fifoRoot;
    private readonly IDbContextFactory<KitharaDbContext> _dbFactory;
    private readonly SourceModuleOrchestrator _orch;
    private readonly ConcurrentDictionary<Guid, ActiveTrackJob> _jobs = new();
    private readonly ILogger<Neck> _logger;

    public Neck(
        IOptions<NeckOptions> options,
        IDbContextFactory<KitharaDbContext> dbFactory,
        SourceModuleOrchestrator orch,
        ILogger<Neck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _orch = orch ?? throw new ArgumentNullException(nameof(orch));
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

    /// <summary>
    /// Creates an alive Struna: persist row + session FIFO (FFmpeg/silence in Phase 4).
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

        await EnsureStrunaFifoAsync(struna.Id, cancellationToken).ConfigureAwait(false);
        return new CreateStrunaOutcome(struna, null);
    }

    /// <summary>
    /// Tears down a Struna: StopTrack if any, remove FIFO, destroy ephemeral guests, free slug.
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

        await StopCurrentTrackAsync(id, cancellationToken).ConfigureAwait(false);
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
    /// Starts (or replaces) a track job. Empty module/trackRef resumes via source <c>ResumeTrack</c>
    /// (Phase 4 silence feeder will own true unpause). Cache miss / download is the source module's job.
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
            if (!_jobs.TryGetValue(strunaId, out var existing))
            {
                return new PlayTrackOutcome(false, null, PlayTrackError.NothingToResume, null);
            }

            var resume = await _orch.ResumeTrackAsync(
                    existing.ModuleSlug,
                    existing.TrackJobId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (resume.Ok)
            {
                return new PlayTrackOutcome(true, existing.TrackJobId, null, null);
            }

            return new PlayTrackOutcome(false, null, PlayTrackError.ModuleFailed, resume.FailureReason);
        }

        await StopCurrentTrackAsync(strunaId, cancellationToken).ConfigureAwait(false);

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

        _jobs[strunaId] = new ActiveTrackJob(start.ModuleSlug!, start.TrackJobId, trackRef.Trim());
        return new PlayTrackOutcome(true, start.TrackJobId, null, null);
    }

    /// <summary>
    /// Pauses the current source track job. True silence feeder (FFmpeg keeps humming) is Phase 4.
    /// </summary>
    public async Task<PlayTrackOutcome> PauseTrackAsync(
        Guid strunaId,
        CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(strunaId, out var job))
        {
            return new PlayTrackOutcome(false, null, PlayTrackError.NothingToResume, "no_active_track");
        }

        var pause = await _orch.PauseTrackAsync(job.ModuleSlug, job.TrackJobId, cancellationToken)
            .ConfigureAwait(false);
        if (!pause.Ok)
        {
            return new PlayTrackOutcome(false, job.TrackJobId, PlayTrackError.ModuleFailed, pause.FailureReason);
        }

        return new PlayTrackOutcome(true, job.TrackJobId, null, null);
    }

    /// <summary>Stop current job and start the next queue entry, if any.</summary>
    public async Task<PlayTrackOutcome> SkipAsync(Guid strunaId, CancellationToken cancellationToken = default)
    {
        await StopCurrentTrackAsync(strunaId, cancellationToken).ConfigureAwait(false);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var next = await db.QueueEntries
            .Include(e => e.Tune)
            .Where(e => e.StrunaId == strunaId)
            .OrderBy(e => e.Position)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (next is null)
        {
            return new PlayTrackOutcome(true, null, null, null);
        }

        db.QueueEntries.Remove(next);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var trackRef = string.IsNullOrWhiteSpace(next.Tune.ExternalId)
            ? next.Tune.Id.ToString("D")
            : next.Tune.ExternalId;
        return await PlayTrackAsync(strunaId, next.Tune.ModuleSlug, trackRef, cancellationToken)
            .ConfigureAwait(false);
    }

    public NowPlayingInfo? GetNowPlaying(Guid strunaId) =>
        _jobs.TryGetValue(strunaId, out var job)
            ? new NowPlayingInfo(job.ModuleSlug, job.TrackRef, job.TrackJobId)
            : null;

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

public sealed record NowPlayingInfo(string ModuleSlug, string TrackRef, string TrackJobId);

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
