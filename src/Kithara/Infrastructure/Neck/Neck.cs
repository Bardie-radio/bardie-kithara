using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Kithara.Infrastructure.Neck;

/// <summary>
/// Stream lifecycle service (Phase 3 stub). Owns per-Struna FIFOs —
/// Kithara-created PCM pipes for the life of an alive Struna.
/// FFmpeg / silence feeder land in Phase 4 on this type.
/// </summary>
public sealed class Neck
{
    // 0666 — shared Compose volume; modules + Kithara both open the pipe.
    private const uint FifoMode = 0x1B6;

    private readonly string _fifoRoot;
    private readonly ILogger<Neck> _logger;

    public Neck(IOptions<NeckOptions> options, ILogger<Neck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
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

    /// <summary>
    /// Ensures the FIFO exists for an alive Struna; returns the path for <c>audio_endpoint</c>.
    /// </summary>
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
