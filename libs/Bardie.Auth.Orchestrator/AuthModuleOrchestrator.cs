using Bardie.Auth.Orchestrator.Catalog;
using Bardie.Auth.Orchestrator.Ports;
using Bardie.ModuleChannel.Channel;
using Microsoft.Extensions.Logging;

namespace Bardie.Auth.Orchestrator;

/// <summary>
/// Auth module orchestrator façade. Phase 1: catalog + ports + dial helpers; Authenticate/SeedAdmin land in Phase 2.
/// </summary>
public sealed class AuthModuleOrchestrator
{
    private readonly IAuthModuleCatalog _catalog;
    private readonly IAuthPersistence _persistence;
    private readonly IModuleGrpcChannelFactory _channelFactory;
    private readonly ILogger<AuthModuleOrchestrator> _logger;

    public AuthModuleOrchestrator(
        IAuthModuleCatalog catalog,
        IAuthPersistence persistence,
        IModuleGrpcChannelFactory channelFactory,
        ILogger<AuthModuleOrchestrator> logger)
    {
        _catalog = catalog;
        _persistence = persistence;
        _channelFactory = channelFactory;
        _logger = logger;
    }

    public IAuthModuleCatalog Catalog => _catalog;

    public IAuthPersistence Persistence => _persistence;

    public IModuleGrpcChannelFactory ChannelFactory => _channelFactory;

    public IReadOnlyCollection<AuthModuleRegistration> GetProviders() => _catalog.List();

    public Task AuthenticateStubAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Authenticate stub — Phase 2");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task RefreshStubAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refresh stub — Phase 2");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task SeedAdminStubAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SeedAdmin stub — Phase 2");
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
