using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace Bardie.Module.Channel.Certificates;

public interface IModuleCertificateIssuer
{
    /// <summary>
    /// Issues a client certificate for <paramref name="slug"/> signed by the host CA.
    /// Used for wire delivery in <see cref="ModuleChannelBootstrapMode.Auto"/> and offline provisioning in <see cref="ModuleChannelBootstrapMode.Preshared"/>.
    /// </summary>
    IssuedClientCertificate IssueClientCertificate(string slug);

    /// <summary>
    /// Writes a client cert+key under the preshared directory for <paramref name="slug"/> (offline admin tooling).
    /// </summary>
    IssuedClientCertificate ProvisionPresharedClientCertificate(string slug);
}

public sealed class ModuleCertificateIssuer : IModuleCertificateIssuer
{
    private readonly FileModuleCertificateStore _store;
    private readonly ModuleChannelOptions _options;

    public ModuleCertificateIssuer(
        IModuleCertificateStore store,
        IOptions<ModuleChannelOptions> options)
    {
        _store = store as FileModuleCertificateStore
            ?? throw new InvalidOperationException(
                $"{nameof(IModuleCertificateIssuer)} requires {nameof(FileModuleCertificateStore)}.");
        _options = options.Value;
    }

    public IssuedClientCertificate IssueClientCertificate(string slug)
    {
        if (!_store.IsLoaded)
        {
            throw new InvalidOperationException("TLS material is not loaded. Call EnsureLoadedAsync first.");
        }

        using var issued = _store.CreateClientCertificate(slug, _options.ClientCertificateLifetime, out var key);
        try
        {
            return new IssuedClientCertificate
            {
                ClientCertificatePem = FileModuleCertificateStore.ExportCertificatePem(issued),
                ClientPrivateKeyPem = FileModuleCertificateStore.ExportPrivateKeyPem(key),
                CaCertificatePem = _store.CaCertificatePem,
                CaThumbprint = _store.CaThumbprint,
                ExpiresAt = issued.NotAfter.ToUniversalTime(),
            };
        }
        finally
        {
            key.Dispose();
        }
    }

    public IssuedClientCertificate ProvisionPresharedClientCertificate(string slug)
    {
        if (string.IsNullOrWhiteSpace(_options.PresharedDir))
        {
            throw new InvalidOperationException(
                "PresharedDir is not configured (BARDIE_MODULE_MTLS_PRESHARED_DIR).");
        }

        var material = IssueClientCertificate(slug);
        var safe = FileModuleCertificateStore.SanitizeSlug(slug);
        var dir = Path.Combine(_options.PresharedDir, safe);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "client.crt.pem"), material.ClientCertificatePem);
        File.WriteAllText(Path.Combine(dir, "client.key.pem"), material.ClientPrivateKeyPem);
        File.WriteAllText(Path.Combine(dir, "ca.crt.pem"), material.CaCertificatePem);
        return material;
    }
}
