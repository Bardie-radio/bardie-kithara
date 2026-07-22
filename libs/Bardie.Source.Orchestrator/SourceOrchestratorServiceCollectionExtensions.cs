using Bardie.ModuleChannel;
using Bardie.Source.Orchestrator.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace Bardie.Source.Orchestrator;

public static class SourceOrchestratorServiceCollectionExtensions
{
    /// <summary>
    /// Registers source orchestrator catalog, façade, and ModuleChannel with mTLS on by default.
    /// Host must register <see cref="Ports.IBlobStorage"/>.
    /// Does not double-register ModuleChannel when <see cref="Auth.Orchestrator.AuthOrchestratorServiceCollectionExtensions.AddAuthModuleOrchestrator"/> already did.
    /// </summary>
    public static IServiceCollection AddSourceModuleOrchestrator(
        this IServiceCollection services,
        Action<ModuleChannelOptions>? configureModuleChannel = null,
        bool registerModuleChannel = true)
    {
        if (registerModuleChannel)
        {
            services.AddModuleChannel(configure: options =>
            {
                options.UseMtls = true;
                configureModuleChannel?.Invoke(options);
            });
        }
        else if (configureModuleChannel is not null)
        {
            services.Configure(configureModuleChannel);
        }

        services.AddSingleton<ISourceModuleCatalog, SourceModuleCatalog>();
        services.AddSingleton<SourceModuleOrchestrator>();
        return services;
    }
}
