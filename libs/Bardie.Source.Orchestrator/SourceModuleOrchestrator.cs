using Bardie.ModuleChannel.Channel;
using Bardie.Source.Orchestrator.Catalog;
using Bardie.Source.Orchestrator.Ports;
using Microsoft.Extensions.Logging;

namespace Bardie.Source.Orchestrator;

/// <summary>
/// Source module orchestrator façade. Phase 1: catalog + ports + dial helpers; Search/StartTrack land in Phase 3.
/// </summary>
public sealed class SourceModuleOrchestrator
{
    private readonly ISourceModuleCatalog _catalog;
    private readonly IBlobStorage _blobStorage;
    private readonly IModuleGrpcChannelFactory _channelFactory;
    private readonly ILogger<SourceModuleOrchestrator> _logger;

    public SourceModuleOrchestrator(
        ISourceModuleCatalog catalog,
        IBlobStorage blobStorage,
        IModuleGrpcChannelFactory channelFactory,
        ILogger<SourceModuleOrchestrator> logger)
    {
        _catalog = catalog;
        _blobStorage = blobStorage;
        _channelFactory = channelFactory;
        _logger = logger;
    }

    public ISourceModuleCatalog Catalog => _catalog;

    public IBlobStorage BlobStorage => _blobStorage;

    public IModuleGrpcChannelFactory ChannelFactory => _channelFactory;

    public IReadOnlyCollection<SourceModuleRegistration> GetSources() => _catalog.List();

    public Task SearchStubAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Search stub — Phase 3");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task StartTrackStubAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("StartTrack stub — Phase 3");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
