using Bardie.Orchestrator.Auth.Catalog;
using Bardie.Module.Channel;
using Microsoft.Extensions.DependencyInjection;

namespace Bardie.Orchestrator.Auth;

public static class AuthOrchestratorServiceCollectionExtensions
{
    /// <summary>
    /// Registers auth orchestrator catalog, façade, and ModuleChannel with mTLS on by default.
    /// Host must register <see cref="Ports.IAuthPersistence"/>.
    /// </summary>
    public static IServiceCollection AddAuthModuleOrchestrator(
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

        services.AddSingleton<IAuthModuleCatalog, AuthModuleCatalog>();
        services.AddSingleton<AuthModuleOrchestrator>();
        return services;
    }
}
