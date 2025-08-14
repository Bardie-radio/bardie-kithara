using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public interface INeckService
{
    Task<string> StartStreamAsync(Guid playlistId, Struna stream, CancellationToken cancellationToken = default);
    Task<bool> StopStreamAsync(Guid streamId);
    Task<bool> UpdatePlaylistAsync(Guid playlistId);
    IEnumerable<Guid> GetActiveStreams();
}

public class NeckService : INeckService
{
    private readonly KitharaDbContext _db;
    private readonly ConcurrentDictionary<Guid, Process> _activeStreams = new();
    private readonly ConcurrentDictionary<Guid, Guid> _streamPlaylists = new();
    private readonly ConcurrentDictionary<Guid, int> _currentTrackIndices = new();
    private readonly Random _rng = new();

    public NeckService(KitharaDbContext db)
    {
        _db = db;
    }

    public IEnumerable<Guid> GetActiveStreams() => _activeStreams.Keys;

    public async Task<string> StartStreamAsync(Guid playlistId, Struna stream, CancellationToken cancellationToken = default)
    {
        if (_activeStreams.ContainsKey(stream.Id))
            return $"Stream {stream.Title} is already running";

        var playlist = await _db.Playlists
            .Include(p => p.Tunes)
            .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);
            
        if (playlist == null || playlist.Tunes == null || !playlist.Tunes.Any())
            return "Playlist not found or empty.";

        var tunes = playlist.Tunes.Where(t => !string.IsNullOrEmpty(t.FilePath)).ToList();
        if (!tunes.Any())
            return "No valid audio files in playlist.";

        // Shuffle playlist
        tunes = tunes.OrderBy(_ => _rng.Next()).ToList();
        var audioFiles = tunes.Select(t => t.FilePath).ToList();

        // Generate metadata file for FFmpeg
        var metadataPath = Path.Combine(Path.GetTempPath(), $"kithara_metadata_{stream.Id}.txt");
        await File.WriteAllLinesAsync(metadataPath, tunes.Select(t => 
            $"title={t.Title}\n" +
            $"artist={t.Artist}\n" +
            $"album={t.Album}\n" +
            $"genre={t.Genre}\n" +
            $"year={t.Year}\n" +
            $"APIC={t.CoverArtUrl}"
        ), cancellationToken);

        // Build FFMPEG input list with metadata
        var inputList = string.Join("|", audioFiles);
        var ffmpegArgs = $"-hide_banner -loglevel error -y " +
                        $"-i \"concat:{inputList}\" " +
                        $"-i \"{metadataPath}\" " +
                        $"-map_metadata 1 " +
                        $"-c:a libmp3lame -b:a 192k " +
                        $"-content_type \"audio/mpeg\" " +
                        $"-ice_name \"{stream.Title}\" " +
                        $"-ice_description \"{stream.Description}\" " +
                        $"-ice_url \"{stream.Url}\" " +
                        $"-ice_genre \"{tunes.FirstOrDefault()?.Genre ?? "Various"}\" " +
                        $"-f mp3 " +
                        $"-icy_metadata 1 " +
                        $"{stream.Url}";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = ffmpegArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.EnableRaisingEvents = true;
        process.Exited += (s, e) =>
        {
            _activeStreams.TryRemove(stream.Id, out _);
            _streamPlaylists.TryRemove(stream.Id, out _);
        };

        process.Start();
        _activeStreams[stream.Id] = process;
        _streamPlaylists[stream.Id] = playlistId;

        // Monitor playlist changes and restart stream if needed
        _ = MonitorPlaylistAsync(playlistId, stream, cancellationToken);

        return $"Stream {stream.Title} started successfully";
    }

    public async Task<bool> StopStreamAsync(Guid streamId)
    {
        if (_activeStreams.TryRemove(streamId, out var process))
        {
            try
            {
                process.Kill();
                process.Dispose();
                _streamPlaylists.TryRemove(streamId, out _);
                return true;
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> UpdatePlaylistAsync(Guid playlistId)
    {
        // Restart all streams using this playlist
        var streamIds = _streamPlaylists.Where(kv => kv.Value == playlistId).Select(kv => kv.Key).ToList();
        foreach (var streamId in streamIds)
        {
            var stream = await _db.Set<Struna>().FindAsync(streamId);
            if (stream != null)
            {
                await StopStreamAsync(streamId);
                await StartStreamAsync(playlistId, stream);
            }
        }
        return true;
    }

    private async Task MonitorPlaylistAsync(Guid playlistId, Struna stream, CancellationToken cancellationToken)
    {
        var lastCount = await _db.Tunes.CountAsync(t => t.PlaylistId == playlistId, cancellationToken);
        
        // Initialize current track index
        _currentTrackIndices.TryAdd(stream.Id, 0);
        
        while (_activeStreams.ContainsKey(stream.Id) && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            
            // Check for playlist changes
            var currentCount = await _db.Tunes.CountAsync(t => t.PlaylistId == playlistId, cancellationToken);
            if (currentCount != lastCount)
            {
                await StopStreamAsync(stream.Id);
                await StartStreamAsync(playlistId, stream, cancellationToken);
                break;
            }

            // Update current track index and metadata
            if (_currentTrackIndices.TryGetValue(stream.Id, out int currentIndex))
            {
                var playlist = await _db.Playlists
                    .Include(p => p.Tunes)
                    .FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);

                if (playlist?.Tunes != null && playlist.Tunes.Any())
                {
                    var tunes = playlist.Tunes.Where(t => !string.IsNullOrEmpty(t.FilePath)).ToList();
                    if (tunes.Any())
                    {
                        currentIndex = (currentIndex + 1) % tunes.Count;
                        _currentTrackIndices[stream.Id] = currentIndex;
                        
                        var currentTune = tunes[currentIndex];
                        if (_activeStreams.TryGetValue(stream.Id, out var process))
                        {
                            // Update ICY metadata
                            var metadata = $"StreamTitle='{currentTune.Title} - {currentTune.Artist}';StreamUrl='{currentTune.CoverArtUrl}'";
                            await process.StandardInput.WriteLineAsync($"ICY-MetaData: {metadata}");
                        }
                    }
                }
            }
            
            lastCount = currentCount;
        }

        // Cleanup
        _currentTrackIndices.TryRemove(stream.Id, out _);
    }
}
