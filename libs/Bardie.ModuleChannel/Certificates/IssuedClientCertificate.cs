namespace Bardie.ModuleChannel.Certificates;

/// <summary>Result of issuing (or loading) a module client certificate.</summary>
public sealed class IssuedClientCertificate
{
    public required string ClientCertificatePem { get; init; }
    public required string ClientPrivateKeyPem { get; init; }
    public required string CaCertificatePem { get; init; }
    public required string CaThumbprint { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
