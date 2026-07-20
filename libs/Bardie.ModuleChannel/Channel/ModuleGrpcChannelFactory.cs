using System.Security.Cryptography.X509Certificates;
using Bardie.ModuleChannel.Certificates;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace Bardie.ModuleChannel.Channel;

/// <summary>Creates outbound gRPC channels that trust the module CA (and optionally present a client cert).</summary>
public interface IModuleGrpcChannelFactory
{
    GrpcChannel CreateChannel(string address, X509Certificate2? clientCertificate = null);
}

public sealed class ModuleGrpcChannelFactory : IModuleGrpcChannelFactory
{
    private readonly IModuleCertificateStore _store;
    private readonly ModuleChannelOptions _options;

    public ModuleGrpcChannelFactory(
        IModuleCertificateStore store,
        IOptions<ModuleChannelOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public GrpcChannel CreateChannel(string address, X509Certificate2? clientCertificate = null)
    {
        if (!_options.UseMtls)
        {
            return GrpcChannel.ForAddress(address);
        }

        if (!_store.IsLoaded)
        {
            throw new InvalidOperationException("TLS material is not loaded. Call EnsureLoadedAsync first.");
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
            {
                if (cert is null)
                {
                    return false;
                }

                using var presented = new X509Certificate2(cert);
                return ValidateAgainstCa(presented);
            },
        };

        if (clientCertificate is not null)
        {
            handler.ClientCertificates.Add(clientCertificate);
        }

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler,
        });
    }

    private bool ValidateAgainstCa(X509Certificate2 certificate)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(_store.CaCertificate);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(certificate);
    }
}
