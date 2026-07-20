using Bardie.Source.Orchestrator.Ports;

namespace Kithara.Infrastructure.Storage;

/// <summary>Phase 1 stub blob storage — real local/S3 drivers land with the source vertical.</summary>
public sealed class StubBlobStorage : IBlobStorage
{
    public Task PutAsync(
        string key,
        Stream content,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Task.FromResult(false);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Task.CompletedTask;
    }
}
