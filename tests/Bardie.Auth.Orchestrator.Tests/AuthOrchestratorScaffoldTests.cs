using Bardie.Auth.Orchestrator;
using Bardie.Auth.Orchestrator.Catalog;
using Bardie.Auth.Orchestrator.Ports;
using Bardie.ModuleChannel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bardie.Auth.Orchestrator.Tests;

public class AuthOrchestratorScaffoldTests
{
    [Fact]
    public void AddAuthModuleOrchestrator_registers_catalog_and_module_channel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthPersistence, FakeAuthPersistence>();
        services.AddAuthModuleOrchestrator(options =>
        {
            options.TlsDataPath = Path.Combine(Path.GetTempPath(), "auth-orch-mtls-" + Guid.NewGuid().ToString("N"));
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IAuthModuleCatalog>());
        Assert.NotNull(provider.GetRequiredService<AuthModuleOrchestrator>());
        Assert.NotNull(provider.GetRequiredService<Bardie.ModuleChannel.Certificates.IModuleCertificateStore>());
    }

    private sealed class FakeAuthPersistence : IAuthPersistence
    {
        public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> CountUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
