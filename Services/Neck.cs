using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public interface INeckService
{
    Task<string> StartStreamAsync(Guid playlistId, string endpointUrl, CancellationToken cancellationToken = default);
    Task<bool> StopStreamAsync(string endpointUrl);
    Task<bool> UpdatePlaylistAsync(Guid playlistId);
    IEnumerable<string> GetActiveStreams();
}

public class NeckService : INeckService
{
    private readonly KitharaDbContext _db;
    private readonly ConcurrentDictionary<string, Process> _activeStreams = new();
    private readonly ConcurrentDictionary<string, Guid> _streamPlaylists = new();
    private readonly Random _rng = new();

    public NeckService(KitharaDbContext db)
    {
        _db = db;
    }

    public IEnumerable<string> GetActiveStreams() => _activeStreams.Keys;

    public async Task<string> StartStreamAsync(Guid playlistId, string endpointUrl, CancellationToken cancellationToken = default)
    {
        if (_activeStreams.ContainsKey(endpointUrl))
            return $"Stream already running at {endpointUrl}";

        var playlist = await _db.Playlists.Include(p => p.Tunes).FirstOrDefaultAsync(p => p.Id == playlistId, cancellationToken);
        if (playlist == null || playlist.Tunes == null || !playlist.Tunes.Any())
            return "Playlist not found or empty.";

        var audioFiles = playlist.Tunes.Select(t => t.FilePath).Where(f => !string.IsNullOrEmpty(f)).ToList();
        if (!audioFiles.Any())
            return "No valid audio files in playlist.";

        // Shuffle playlist
        audioFiles = audioFiles.OrderBy(_ => _rng.Next()).ToList();

        // Build FFMPEG input list
        var inputList = string.Join("|", audioFiles);
        var ffmpegArgs = $"-hide_banner -loglevel error -y -i \"concat:{inputList}\" -f mp3 {endpointUrl}";

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
            _activeStreams.TryRemove(endpointUrl, out _);
            _streamPlaylists.TryRemove(endpointUrl, out _);
        };

        process.Start();
        _activeStreams[endpointUrl] = process;
        _streamPlaylists[endpointUrl] = playlistId;

        // Optionally: monitor playlist changes and restart stream
        _ = MonitorPlaylistAsync(playlistId, endpointUrl, cancellationToken);

        return $"Stream started at {endpointUrl}";
    }

    public async Task<bool> StopStreamAsync(string endpointUrl)
    {
        if (_activeStreams.TryRemove(endpointUrl, out var process))
        {
            try
            {
                process.Kill();
                process.Dispose();
                _streamPlaylists.TryRemove(endpointUrl, out _);
                return true;
            }
            catch { }
        }
        return false;
    }

    public async Task<bool> UpdatePlaylistAsync(Guid playlistId)
    {
        // Restart all streams using this playlist
        var endpoints = _streamPlaylists.Where(kv => kv.Value == playlistId).Select(kv => kv.Key).ToList();
        foreach (var endpoint in endpoints)
        {
            await StopStreamAsync(endpoint);
            await StartStreamAsync(playlistId, endpoint);
        }
        return true;
    }

    private async Task MonitorPlaylistAsync(Guid playlistId, string endpointUrl, CancellationToken cancellationToken)
    {
        var lastCount = await _db.Tunes.CountAsync(t => t.PlaylistId == playlistId, cancellationToken);
        while (_activeStreams.ContainsKey(endpointUrl) && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            var currentCount = await _db.Tunes.CountAsync(t => t.PlaylistId == playlistId, cancellationToken);
            if (currentCount != lastCount)
            {
                await StopStreamAsync(endpointUrl);
                await StartStreamAsync(playlistId, endpointUrl, cancellationToken);
                break;
            }
            lastCount = currentCount;
        }
    }
}
