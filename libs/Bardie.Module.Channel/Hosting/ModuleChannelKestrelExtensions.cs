using Bardie.Module.Channel.Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;

namespace Bardie.Module.Channel.Hosting;

public static class ModuleChannelKestrelExtensions
{
    /// <summary>
    /// Binds HTTPS + optional client certificates on a listen endpoint for module gRPC.
    /// Uses <see cref="ClientCertificateMode.AllowCertificate"/> so Register can proceed without a client cert;
    /// privileged RPCs enforce mTLS via <see cref="ModuleChannelBootstrapInterceptor"/>.
    /// </summary>
    public static ListenOptions UseBardieModuleGrpc(this ListenOptions listenOptions)
    {
        var store = listenOptions.ApplicationServices.GetRequiredService<IModuleCertificateStore>();
        if (!store.IsLoaded)
        {
            store.EnsureLoadedAsync().GetAwaiter().GetResult();
        }

        var options = listenOptions.ApplicationServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ModuleChannelOptions>>()
            .Value;

        listenOptions.Protocols = HttpProtocols.Http2;

        if (!options.UseMtls)
        {
            return listenOptions;
        }

        listenOptions.UseHttps(https =>
        {
            https.ServerCertificate = store.ServerCertificate;
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.AllowAnyClientCertificate();
        });

        return listenOptions;
    }

    /// <summary>
    /// Configures Kestrel dual listeners: plain HTTP on <paramref name="httpPort"/> and module gRPC TLS on <paramref name="grpcPort"/>.
    /// </summary>
    public static void ConfigureBardieModuleListeners(
        this KestrelServerOptions kestrel,
        int httpPort = 8080,
        int grpcPort = 5000)
    {
        kestrel.ListenAnyIP(httpPort, listen =>
        {
            // Public HTTP control/health — HTTP/1.1 only (HTTP/2 needs TLS).
            listen.Protocols = HttpProtocols.Http1;
        });

        kestrel.ListenAnyIP(grpcPort, listen => listen.UseBardieModuleGrpc());
    }
}
