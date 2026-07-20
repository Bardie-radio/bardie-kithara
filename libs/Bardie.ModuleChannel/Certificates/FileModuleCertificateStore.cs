using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bardie.ModuleChannel.Certificates;

public sealed class FileModuleCertificateStore : IModuleCertificateStore, IDisposable
{
    private readonly ModuleChannelOptions _options;
    private readonly ILogger<FileModuleCertificateStore> _logger;
    private readonly object _gate = new();
    private X509Certificate2? _ca;
    private X509Certificate2? _server;
    private string? _caPem;
    private bool _disposed;

    public FileModuleCertificateStore(
        IOptions<ModuleChannelOptions> options,
        ILogger<FileModuleCertificateStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsLoaded => _ca is not null && _server is not null;

    public X509Certificate2 ServerCertificate =>
        _server ?? throw new InvalidOperationException("TLS material is not loaded. Call EnsureLoadedAsync first.");

    public X509Certificate2 CaCertificate =>
        _ca ?? throw new InvalidOperationException("TLS material is not loaded. Call EnsureLoadedAsync first.");

    public string CaCertificatePem =>
        _caPem ?? throw new InvalidOperationException("TLS material is not loaded. Call EnsureLoadedAsync first.");

    public string CaThumbprint => CaCertificate.Thumbprint;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded)
        {
            return;
        }

        await Task.Run(() =>
        {
            lock (_gate)
            {
                if (IsLoaded)
                {
                    return;
                }

                Directory.CreateDirectory(_options.TlsDataPath);
                var caCertPath = Path.Combine(_options.TlsDataPath, "ca.crt.pem");
                var caKeyPath = Path.Combine(_options.TlsDataPath, "ca.key.pem");
                var serverCertPath = Path.Combine(_options.TlsDataPath, "server.crt.pem");
                var serverKeyPath = Path.Combine(_options.TlsDataPath, "server.key.pem");

                if (File.Exists(caCertPath) && File.Exists(caKeyPath))
                {
                    _ca = LoadCertificateWithKey(caCertPath, caKeyPath);
                    _logger.LogInformation("Loaded module CA from {Path}", caCertPath);
                }
                else
                {
                    _ca = CreateSelfSignedCa();
                    WritePemPair(caCertPath, caKeyPath, _ca);
                    _logger.LogInformation("Generated module CA at {Path}", caCertPath);
                }

                if (File.Exists(serverCertPath) && File.Exists(serverKeyPath))
                {
                    _server = LoadCertificateWithKey(serverCertPath, serverKeyPath);
                    _logger.LogInformation("Loaded module gRPC server cert from {Path}", serverCertPath);
                }
                else
                {
                    _server = CreateServerCertificate(_ca);
                    WritePemPair(serverCertPath, serverKeyPath, _server);
                    _logger.LogInformation("Generated module gRPC server cert at {Path}", serverCertPath);
                }

                _caPem = ExportCertificatePem(_ca);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public bool TryGetPresharedClientMaterial(string slug, out X509Certificate2? certificate)
    {
        certificate = null;
        var dir = ResolvePresharedSlugDir(slug);
        if (dir is null)
        {
            return false;
        }

        var certPath = Path.Combine(dir, "client.crt.pem");
        var keyPath = Path.Combine(dir, "client.key.pem");
        if (!File.Exists(certPath) || !File.Exists(keyPath))
        {
            return false;
        }

        certificate = LoadCertificateWithKey(certPath, keyPath);
        return true;
    }

    public bool TryGetPresharedClientExpiry(string slug, out DateTimeOffset expiresAt)
    {
        expiresAt = default;
        if (!TryGetPresharedClientMaterial(slug, out var cert) || cert is null)
        {
            return false;
        }

        using (cert)
        {
            expiresAt = cert.NotAfter.ToUniversalTime();
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _server?.Dispose();
        _ca?.Dispose();
    }

    private string? ResolvePresharedSlugDir(string slug)
    {
        if (string.IsNullOrWhiteSpace(_options.PresharedDir))
        {
            return null;
        }

        var safe = SanitizeSlug(slug);
        return Path.Combine(_options.PresharedDir, safe);
    }

    internal X509Certificate2 CreateClientCertificate(string slug, TimeSpan lifetime, out RSA privateKey)
    {
        if (_ca is null)
        {
            throw new InvalidOperationException("TLS material is not loaded. Call EnsureLoadedAsync first.");
        }

        privateKey = RSA.Create(2048);
        var subject = new X500DistinguishedName($"CN={SanitizeSlug(slug)}");
        var request = new CertificateRequest(subject, privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.2")],
                true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.Add(lifetime);
        var serial = RandomNumberGenerator.GetBytes(16);
        return request.Create(_ca, notBefore, notAfter, serial);
    }

    private X509Certificate2 CreateSelfSignedCa()
    {
        using var rsa = RSA.Create(4096);
        var request = new CertificateRequest(
            "CN=Bardie Module CA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature,
                true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = notBefore.AddYears(10);
        var cert = request.CreateSelfSigned(notBefore, notAfter);
        return PersistWithPrivateKey(cert, rsa);
    }

    private X509Certificate2 CreateServerCertificate(X509Certificate2 ca)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=kithara",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")],
                true));

        var san = new SubjectAlternativeNameBuilder();
        foreach (var dns in _options.ServerDnsNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            san.AddDnsName(dns);
        }

        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.AddYears(5);
        var serial = RandomNumberGenerator.GetBytes(16);
        using var issued = request.Create(ca, notBefore, notAfter, serial);
        return PersistWithPrivateKey(issued, rsa);
    }

    private static X509Certificate2 PersistWithPrivateKey(X509Certificate2 cert, RSA key)
    {
        // CreateSelfSigned already attaches the key; CertificateRequest.Create does not.
        if (cert.HasPrivateKey)
        {
            var selfSignedPfx = cert.Export(X509ContentType.Pfx, string.Empty);
            return X509CertificateLoader.LoadPkcs12(selfSignedPfx, string.Empty, X509KeyStorageFlags.Exportable);
        }

        using var withKey = cert.CopyWithPrivateKey(key);
        var pfx = withKey.Export(X509ContentType.Pfx, string.Empty);
        return X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.Exportable);
    }

    private static X509Certificate2 LoadCertificateWithKey(string certPath, string keyPath)
    {
        var certPem = File.ReadAllText(certPath);
        var keyPem = File.ReadAllText(keyPath);
        using var cert = X509Certificate2.CreateFromPem(certPem, keyPem);
        var pfx = cert.Export(X509ContentType.Pfx, string.Empty);
        return X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.Exportable);
    }

    private static void WritePemPair(string certPath, string keyPath, X509Certificate2 cert)
    {
        File.WriteAllText(certPath, ExportCertificatePem(cert), Encoding.ASCII);
        using var key = cert.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("Certificate has no RSA private key.");
        File.WriteAllText(keyPath, key.ExportPkcs8PrivateKeyPem(), Encoding.ASCII);
    }

    internal static string ExportCertificatePem(X509Certificate2 cert) =>
        PemEncoding.WriteString("CERTIFICATE", cert.RawData);

    internal static string ExportPrivateKeyPem(RSA key) => key.ExportPkcs8PrivateKeyPem();

    internal static string SanitizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Module slug is required.", nameof(slug));
        }

        var trimmed = slug.Trim().ToLowerInvariant();
        foreach (var ch in trimmed)
        {
            if (!(char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.'))
            {
                throw new ArgumentException($"Invalid module slug '{slug}'.", nameof(slug));
            }
        }

        return trimmed;
    }
}
