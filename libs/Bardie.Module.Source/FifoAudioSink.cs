namespace Bardie.Module.Source;

/// <summary>Writes PCM bytes to a Kithara-owned session FIFO (<c>audio_endpoint</c>).</summary>
public interface IFifoAudioSink
{
    /// <summary>
    /// Opens <paramref name="audioEndpoint"/> for write and copies <paramref name="pcm"/> until EOF or cancel.
    /// Blocks until a reader attaches on a real FIFO (Unix <c>mkfifo</c>).
    /// When <paramref name="isPaused"/> returns true, writing pauses until it returns false.
    /// </summary>
    Task WriteAsync(
        string audioEndpoint,
        Stream pcm,
        CancellationToken cancellationToken = default,
        Func<bool>? isPaused = null);
}

public sealed class FifoAudioSink : IFifoAudioSink
{
    private const int BufferSize = 16 * 1024;
    private static readonly TimeSpan PausePoll = TimeSpan.FromMilliseconds(50);

    public async Task WriteAsync(
        string audioEndpoint,
        Stream pcm,
        CancellationToken cancellationToken = default,
        Func<bool>? isPaused = null)
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

        var buffer = new byte[BufferSize];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (isPaused?.Invoke() == true)
            {
                await Task.Delay(PausePoll, cancellationToken).ConfigureAwait(false);
            }

            var read = await pcm.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                .ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            await fifo.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        await fifo.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
