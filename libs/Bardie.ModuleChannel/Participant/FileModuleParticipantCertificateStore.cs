using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Bardie.ModuleChannel.Certificates;
using Bardie.Modules.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bardie.ModuleChannel.Participant;

/// <summary>Persists module-side mesh TLS material (CA + client from Register; local work-port server cert).</summary>
public interface IModuleParticipantCertificateStore
{
    bool IsClientMaterialLoaded { get; }

    bool IsServerMaterialLoaded { get; }

    X509Certificate2 CaCertificate { get; }

    X509Certificate2 ClientCertificate { get; }

    X509Certificate2 ServerCertificate { get; }

    string CaCertificatePem { get; }

    /// <summary>
    /// Independent copy of the work-port server cert for Kestrel bind (loaded from PEM).
    /// Do not Export <see cref="ServerCertificate"/>.
    /// </summary>
    X509Certificate2 OpenListenerServerCertificate();

    /// <summary>Loads existing PEMs from <see cref="ModuleParticipantOptions.TlsDataPath"/> when present.</summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists CA (+ client cert/key in auto mode) from <see cref="RegisterResponse"/> and reloads in-memory material.
    /// </summary>
    Task ApplyRegisterResponseAsync(RegisterResponse response, CancellationToken cancellationToken = default);

    /// <summary>Ensures a work-port server certificate exists (self-signed local identity for HTTPS listen).</summary>
    Task EnsureServerCertificateAsync(IEnumerable<string>? dnsNames = null, CancellationToken cancellationToken = default);
}

public sealed class FileModuleParticipantCertificateStore : IModuleParticipantCertificateStore, IDisposable
{
    private readonly ModuleParticipantOptions _options;
    private readonly ILogger<FileModuleParticipantCertificateStore> _logger;
    private readonly object _gate = new();
    private X509Certificate2? _ca;
    private X509Certificate2? _client;
    private X509Certificate2? _server;
    private string? _caPem;
    private string? _serverCertPath;
    private string? _serverKeyPath;
    private bool _disposed;

    public FileModuleParticipantCertificateStore(
        IOptions<ModuleParticipantOptions> options,
        ILogger<FileModuleParticipantCertificateStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsClientMaterialLoaded => _ca is not null && _client is not null;

    public bool IsServerMaterialLoaded => _server is not null;

    public X509Certificate2 CaCertificate =>
        _ca ?? throw new InvalidOperationException("Module CA is not loaded.");

    public X509Certificate2 ClientCertificate =>
        _client ?? throw new InvalidOperationException("Module client certificate is not loaded.");

    public X509Certificate2 ServerCertificate =>
        _server ?? throw new InvalidOperationException("Module server certificate is not loaded. Call EnsureServerCertificateAsync first.");

    public string CaCertificatePem =>
        _caPem ?? throw new InvalidOperationException("Module CA is not loaded.");

    public X509Certificate2 OpenListenerServerCertificate()
    {
        if (_serverCertPath is null || _serverKeyPath is null || _server is null)
        {
            throw new InvalidOperationException("Module server certificate is not loaded. Call EnsureServerCertificateAsync first.");
        }

        return CertificateMaterial.LoadPemPair(_serverCertPath, _serverKeyPath);
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_gate)
            {
                Directory.CreateDirectory(_options.TlsDataPath);
                TryLoadCa();
                TryLoadClient();
                TryLoadServer();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task ApplyRegisterResponseAsync(RegisterResponse response, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        await Task.Run(() =>
        {
            lock (_gate)
            {
                Directory.CreateDirectory(_options.TlsDataPath);

                if (string.IsNullOrWhiteSpace(response.CaCertificatePem))
                {
                    throw new InvalidOperationException("RegisterResponse.CaCertificatePem is required.");
                }

                var caPath = Path.Combine(_options.TlsDataPath, "ca.crt.pem");
                File.WriteAllText(caPath, response.CaCertificatePem.Trim() + Environment.NewLine, Encoding.ASCII);
                _logger.LogInformation("Persisted module mesh CA to {Path}", caPath);

                if (!string.IsNullOrWhiteSpace(response.ClientCertificatePem)
                    && !string.IsNullOrWhiteSpace(response.ClientPrivateKeyPem))
                {
                    var certPath = Path.Combine(_options.TlsDataPath, "client.crt.pem");
                    var keyPath = Path.Combine(_options.TlsDataPath, "client.key.pem");
                    File.WriteAllText(certPath, response.ClientCertificatePem.Trim() + Environment.NewLine, Encoding.ASCII);
                    File.WriteAllText(keyPath, response.ClientPrivateKeyPem.Trim() + Environment.NewLine, Encoding.ASCII);
                    CertificateMaterial.RestrictKeyFilePermissions(keyPath);
                    _logger.LogInformation("Persisted auto-mode module client cert to {Path}", certPath);
                }
                else
                {
                    _logger.LogInformation(
                        "RegisterResponse had no client private key PEMs (preshared mode). Expecting client material under {Path}.",
                        _options.TlsDataPath);
                }

                DisposeCert(ref _ca);
                DisposeCert(ref _client);
                TryLoadCa();
                TryLoadClient();
            }
        }, cancellationToken).ConfigureAwait(false);

        if (!IsClientMaterialLoaded)
        {
            throw new InvalidOperationException(
                "Client certificate material is missing after Register. In preshared mode, place client.crt.pem + client.key.pem under TlsDataPath before start.");
        }
    }

    public async Task EnsureServerCertificateAsync(
        IEnumerable<string>? dnsNames = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_gate)
            {
                Directory.CreateDirectory(_options.TlsDataPath);
                TryLoadServer();
                if (_server is not null)
                {
                    return;
                }

                var names = (dnsNames ?? _options.ServerDnsNames)
                    .Where(static n => !string.IsNullOrWhiteSpace(n))
                    .Select(static n => n.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .DefaultIfEmpty("localhost")
                    .ToArray();

                _serverCertPath = Path.Combine(_options.TlsDataPath, "server.crt.pem");
                _serverKeyPath = Path.Combine(_options.TlsDataPath, "server.key.pem");

                using (var generated = CreateSelfSignedServerCertificate(names))
                {
                    CertificateMaterial.WritePemPair(_serverCertPath, _serverKeyPath, generated);
                }

                _server = CertificateMaterial.LoadPemPair(_serverCertPath, _serverKeyPath);
                _logger.LogInformation("Generated module work-port server cert at {Path}", _serverCertPath);
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DisposeCert(ref _server);
        DisposeCert(ref _client);
        DisposeCert(ref _ca);
    }

    private void TryLoadCa()
    {
        var caPath = Path.Combine(_options.TlsDataPath, "ca.crt.pem");
        if (!File.Exists(caPath))
        {
            return;
        }

        DisposeCert(ref _ca);
        var pem = File.ReadAllText(caPath);
        _ca = X509Certificate2.CreateFromPem(pem);
        _caPem = pem.Contains("BEGIN CERTIFICATE", StringComparison.Ordinal)
            ? pem.Trim()
            : CertificateMaterial.ExportCertificatePem(_ca);
        _logger.LogDebug("Loaded module mesh CA from {Path}", caPath);
    }

    private void TryLoadClient()
    {
        var certPath = Path.Combine(_options.TlsDataPath, "client.crt.pem");
        var keyPath = Path.Combine(_options.TlsDataPath, "client.key.pem");
        if (!File.Exists(certPath) || !File.Exists(keyPath))
        {
            return;
        }

        DisposeCert(ref _client);
        _client = CertificateMaterial.LoadPemPair(certPath, keyPath);
        CertificateMaterial.RestrictKeyFilePermissions(keyPath);
        _logger.LogDebug("Loaded module client cert from {Path}", certPath);
    }

    private void TryLoadServer()
    {
        _serverCertPath = Path.Combine(_options.TlsDataPath, "server.crt.pem");
        _serverKeyPath = Path.Combine(_options.TlsDataPath, "server.key.pem");
        if (!File.Exists(_serverCertPath) || !File.Exists(_serverKeyPath))
        {
            return;
        }

        DisposeCert(ref _server);
        _server = CertificateMaterial.LoadPemPair(_serverCertPath, _serverKeyPath);
        CertificateMaterial.RestrictKeyFilePermissions(_serverKeyPath);
        _logger.LogDebug("Loaded module work-port server cert from {Path}", _serverCertPath);
    }

    private static X509Certificate2 CreateSelfSignedServerCertificate(string[] dnsNames)
    {
        using var rsa = RSA.Create(2048);
        var cn = dnsNames[0];
        var request = new CertificateRequest(
            $"CN={cn}",
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
        foreach (var dns in dnsNames)
        {
            san.AddDnsName(dns);
        }

        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = notBefore.AddYears(5);
        var cert = request.CreateSelfSigned(notBefore, notAfter);
        return CertificateMaterial.PersistWithPrivateKey(cert, rsa);
    }

    private static void DisposeCert(ref X509Certificate2? cert)
    {
        cert?.Dispose();
        cert = null;
    }
}
