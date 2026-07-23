using Bardie.Module.Channel.Certificates;
using Bardie.Module.Channel.Channel;
using Bardie.Module.Channel.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bardie.Module.Channel;

public static class ModuleChannelServiceCollectionExtensions
{
    /// <summary>
    /// Registers ModuleChannel mTLS store, issuer, validator, outbound channel factory, and bootstrap interceptor.
    /// <see cref="ModuleChannelOptions.UseMtls"/> defaults to <c>true</c>.
    /// </summary>
    public static IServiceCollection AddModuleChannel(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<ModuleChannelOptions>? configure = null)
    {
        if (configuration is not null)
        {
            services.Configure<ModuleChannelOptions>(configuration.GetSection(ModuleChannelOptions.SectionName));
            ApplyEnvironmentOverrides(services, configuration);
        }
        else
        {
            services.AddOptions<ModuleChannelOptions>();
        }

        if (configure is not null)
        {
            services.Configure(configure);
        }

        // Options Bind/Configure replaces List properties wholesale — re-apply mesh Register unless opted out.
        services.PostConfigure<ModuleChannelOptions>(options =>
        {
            if (!options.IncludeRegisterWithoutClientCertificate)
            {
                return;
            }

            options.AllowMethodWithoutClientCertificate(ModuleRegistryMethodPaths.Register);
        });

        services.AddSingleton<FileModuleCertificateStore>();
        services.AddSingleton<IModuleCertificateStore>(sp => sp.GetRequiredService<FileModuleCertificateStore>());
        services.AddSingleton<IModuleCertificateIssuer, ModuleCertificateIssuer>();
        services.AddSingleton<IModuleCertificateValidator, ModuleCertificateValidator>();
        services.AddSingleton<IModuleGrpcChannelFactory, ModuleGrpcChannelFactory>();
        services.AddSingleton<ModuleChannelBootstrapInterceptor>();

        return services;
    }

    private static void ApplyEnvironmentOverrides(IServiceCollection services, IConfiguration configuration)
    {
        services.PostConfigure<ModuleChannelOptions>(options =>
        {
            var bootstrap = configuration["BARDIE_MODULE_MTLS_BOOTSTRAP"]
                ?? configuration["ModuleChannel:BootstrapMode"];
            if (!string.IsNullOrWhiteSpace(bootstrap)
                && Enum.TryParse<ModuleChannelBootstrapMode>(bootstrap, ignoreCase: true, out var mode))
            {
                options.BootstrapMode = mode;
            }

            var tlsPath = configuration["BARDIE_GRPC_TLS_DATA_PATH"]
                ?? configuration["ModuleChannel:TlsDataPath"];
            if (!string.IsNullOrWhiteSpace(tlsPath))
            {
                options.TlsDataPath = tlsPath;
            }

            var preshared = configuration["BARDIE_MODULE_MTLS_PRESHARED_DIR"]
                ?? configuration["ModuleChannel:PresharedDir"];
            if (!string.IsNullOrWhiteSpace(preshared))
            {
                options.PresharedDir = preshared;
            }

            var useMtls = configuration["ModuleChannel:UseMtls"];
            if (bool.TryParse(useMtls, out var parsed))
            {
                options.UseMtls = parsed;
            }
        });
    }
}
