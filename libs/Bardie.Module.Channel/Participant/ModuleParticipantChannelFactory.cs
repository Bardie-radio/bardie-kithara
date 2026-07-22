using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;

namespace Bardie.Module.Channel.Participant;

/// <summary>Creates outbound gRPC channels from a module process to Kithara.</summary>
public interface IModuleParticipantChannelFactory
{
    /// <summary>
    /// Bootstrap dial for Register: accepts any server certificate (CA unknown until RegisterResponse).
    /// Do not use for Heartbeat or work RPCs.
    /// </summary>
    GrpcChannel CreateBootstrapChannel(string address);

    /// <summary>
    /// Steady-state mTLS dial: trust mesh CA and present the module client certificate.
    /// </summary>
    GrpcChannel CreateMtlsChannel(string address);
}

public sealed class ModuleParticipantChannelFactory : IModuleParticipantChannelFactory
{
    private readonly IModuleParticipantCertificateStore _store;

    public ModuleParticipantChannelFactory(IModuleParticipantCertificateStore store)
    {
        _store = store;
    }

    public GrpcChannel CreateBootstrapChannel(string address)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = static (_, _, _, _) => true,
        };

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler,
        });
    }

    public GrpcChannel CreateMtlsChannel(string address)
    {
        if (!_store.IsClientMaterialLoaded)
        {
            throw new InvalidOperationException(
                "Module client TLS material is not loaded. Complete Register (or place preshared PEMs) first.");
        }

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_store.ClientCertificate);
        handler.ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
        {
            if (cert is null)
            {
                return false;
            }

            using var presented = new X509Certificate2(cert);
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(_store.CaCertificate);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            return chain.Build(presented);
        };

        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler,
        });
    }
}
