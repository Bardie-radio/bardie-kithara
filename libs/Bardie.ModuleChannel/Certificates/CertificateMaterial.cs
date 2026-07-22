using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bardie.ModuleChannel.Certificates;

/// <summary>
/// Shared PEM ↔ <see cref="X509Certificate2"/> helpers for mesh TLS.
/// <para>
/// Linux/.NET gotcha: do not <c>Export</c> a cert instance that Kestrel (or another dial) still holds —
/// that can dispose the private-key handle. Load independent instances from PEM instead.
/// </para>
/// <para>
/// Keys use <see cref="X509KeyStorageFlags.EphemeralKeySet"/> (process memory only, no OS keystore).
/// Private-key PEM files are written with owner-only permissions when the OS supports it.
/// Prefer short-lived outbound copies (<see cref="IModuleCertificateStore.OpenOutboundClientIdentity"/>)
/// over caching extra private-key handles.
/// </para>
/// </summary>
internal static class CertificateMaterial
{
    private static readonly X509KeyStorageFlags EphemeralFlags =
        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet;

    /// <summary>Load cert+key PEMs into an ephemeral in-memory certificate.</summary>
    public static X509Certificate2 LoadPemPair(string certPath, string keyPath)
    {
        var certPem = File.ReadAllText(certPath);
        var keyPem = File.ReadAllText(keyPath);
        using var fromPem = X509Certificate2.CreateFromPem(certPem, keyPem);
        return ToEphemeral(fromPem);
    }

    /// <summary>
    /// Re-materialize <paramref name="cert"/> as an ephemeral instance (PFX round-trip).
    /// Does not dispose <paramref name="cert"/>. Prefer <see cref="LoadPemPair"/> when paths exist.
    /// </summary>
    public static X509Certificate2 ToEphemeral(X509Certificate2 cert)
    {
        var pfx = cert.Export(X509ContentType.Pfx, string.Empty);
        try
        {
            return X509CertificateLoader.LoadPkcs12(pfx, string.Empty, EphemeralFlags);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx.AsSpan());
        }
    }

    /// <summary>Attach <paramref name="key"/> and return an ephemeral cert (disposes intermediate copies).</summary>
    public static X509Certificate2 PersistWithPrivateKey(X509Certificate2 cert, RSA key)
    {
        if (cert.HasPrivateKey)
        {
            using (cert)
            {
                return ToEphemeral(cert);
            }
        }

        using (cert)
        using (var withKey = cert.CopyWithPrivateKey(key))
        {
            return ToEphemeral(withKey);
        }
    }

    public static void WritePemPair(string certPath, string keyPath, X509Certificate2 cert)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(certPath))!);
        File.WriteAllText(certPath, ExportCertificatePem(cert), Encoding.ASCII);
        using var key = cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate has no RSA private key.");
        File.WriteAllText(keyPath, key.ExportPkcs8PrivateKeyPem(), Encoding.ASCII);
        RestrictKeyFilePermissions(keyPath);
    }

    public static string ExportCertificatePem(X509Certificate2 cert) =>
        PemEncoding.WriteString("CERTIFICATE", cert.RawData);

    public static void RestrictKeyFilePermissions(string keyPath)
    {
        if (!OperatingSystem.IsWindows() && File.Exists(keyPath))
        {
            File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
