namespace Bardie.Source.Orchestrator.Ports;

/// <summary>
/// Host storage port for library blob put/get. Phase 1 stub; drivers land with source vertical.
/// </summary>
public interface IBlobStorage
{
    Task PutAsync(string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
