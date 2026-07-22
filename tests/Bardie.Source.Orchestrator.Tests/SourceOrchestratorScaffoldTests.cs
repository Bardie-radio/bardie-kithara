using Bardie.ModuleChannel;
using Bardie.Source.Orchestrator;
using Bardie.Source.Orchestrator.Catalog;
using Bardie.Source.Orchestrator.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bardie.Source.Orchestrator.Tests;

public class SourceOrchestratorScaffoldTests
{
    [Fact]
    public void AddSourceModuleOrchestrator_registers_catalog_and_module_channel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IBlobStorage, FakeBlobStorage>();
        services.AddSourceModuleOrchestrator(options =>
        {
            options.TlsDataPath = Path.Combine(Path.GetTempPath(), "src-orch-mtls-" + Guid.NewGuid().ToString("N"));
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<ISourceModuleCatalog>());
        Assert.NotNull(provider.GetRequiredService<SourceModuleOrchestrator>());
        Assert.NotNull(provider.GetRequiredService<Bardie.ModuleChannel.Certificates.IModuleCertificateStore>());
    }

    private sealed class FakeBlobStorage : IBlobStorage
    {
        public Task<long> PutAsync(
            string key,
            Stream content,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<BlobReadResult?> OpenReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<BlobReadResult?>(null);

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
