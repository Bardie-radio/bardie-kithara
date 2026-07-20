using System.Security.Cryptography.X509Certificates;

namespace Bardie.ModuleChannel.Certificates;

/// <summary>Loads or generates host CA + server TLS material under <see cref="ModuleChannelOptions.TlsDataPath"/>.</summary>
public interface IModuleCertificateStore
{
    /// <summary>Ensures CA and server certificates exist on disk and are loaded into memory.</summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    bool IsLoaded { get; }

    X509Certificate2 ServerCertificate { get; }

    X509Certificate2 CaCertificate { get; }

    string CaCertificatePem { get; }

    string CaThumbprint { get; }

    /// <summary>
    /// Returns true when a pre-placed client cert (+ key) for <paramref name="slug"/> exists under the preshared directory.
    /// </summary>
    bool TryGetPresharedClientMaterial(string slug, out X509Certificate2? certificate);

    /// <summary>Optional metadata for a preshared client cert (expiry) without exposing private key PEMs.</summary>
    bool TryGetPresharedClientExpiry(string slug, out DateTimeOffset expiresAt);
}
