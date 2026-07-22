namespace Bardie.Module.Source;

/// <summary>Writes PCM bytes to a Kithara-owned session FIFO (<c>audio_endpoint</c>).</summary>
public interface IFifoAudioSink
{
    /// <summary>
    /// Opens <paramref name="audioEndpoint"/> for write and copies <paramref name="pcm"/> until EOF or cancel.
    /// Blocks until a reader attaches on a real FIFO (Unix <c>mkfifo</c>).
    /// </summary>
    Task WriteAsync(
        string audioEndpoint,
        Stream pcm,
        CancellationToken cancellationToken = default);
}

public sealed class FifoAudioSink : IFifoAudioSink
{
    private const int BufferSize = 64 * 1024;

    public async Task WriteAsync(
        string audioEndpoint,
        Stream pcm,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioEndpoint);
        ArgumentNullException.ThrowIfNull(pcm);

        await using var fifo = new FileStream(
            audioEndpoint,
            FileMode.Open,
            FileAccess.Write,
            FileShare.ReadWrite,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await pcm.CopyToAsync(fifo, BufferSize, cancellationToken).ConfigureAwait(false);
        await fifo.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
