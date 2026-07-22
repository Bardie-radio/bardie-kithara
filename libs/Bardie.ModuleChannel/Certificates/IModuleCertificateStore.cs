using System.Security.Cryptography.X509Certificates;

namespace Bardie.ModuleChannel.Certificates;

/// <summary>Loads or generates host CA + server TLS material under <see cref="ModuleChannelOptions.TlsDataPath"/>.</summary>
public interface IModuleCertificateStore
{
    /// <summary>Ensures CA and server certificates exist on disk and are loaded into memory.</summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    bool IsLoaded { get; }

    /// <summary>Kestrel-bound host server certificate. Do not Export or dispose this instance for dialing.</summary>
    X509Certificate2 ServerCertificate { get; }

    X509Certificate2 CaCertificate { get; }

    string CaCertificatePem { get; }

    string CaThumbprint { get; }

    /// <summary>
    /// Short-lived host identity for one outbound host→module mTLS dial (loaded from PEM).
    /// Caller or channel factory must <see cref="IDisposable.Dispose"/> — do not cache.
    /// </summary>
    X509Certificate2 OpenOutboundClientIdentity();

    /// <summary>
    /// Returns true when a pre-placed client cert (+ key) for <paramref name="slug"/> exists under the preshared directory.
    /// </summary>
    bool TryGetPresharedClientMaterial(string slug, out X509Certificate2? certificate);

    /// <summary>Optional metadata for a preshared client cert (expiry) without exposing private key PEMs.</summary>
    bool TryGetPresharedClientExpiry(string slug, out DateTimeOffset expiresAt);
}
