using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Bardie.Module.Channel.Certificates;

public interface IModuleCertificateValidator
{
    /// <summary>
    /// Validates that <paramref name="certificate"/> was issued by the module CA and extracts the module slug (CN).
    /// </summary>
    bool TryValidate(X509Certificate2? certificate, out string slug);
}

public sealed class ModuleCertificateValidator : IModuleCertificateValidator
{
    private readonly IModuleCertificateStore _store;

    public ModuleCertificateValidator(IModuleCertificateStore store)
    {
        _store = store;
    }

    public bool TryValidate(X509Certificate2? certificate, out string slug)
    {
        slug = string.Empty;
        if (certificate is null || !_store.IsLoaded)
        {
            return false;
        }

        try
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(_store.CaCertificate);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            if (!chain.Build(certificate))
            {
                return false;
            }

            var cn = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
            if (string.IsNullOrWhiteSpace(cn))
            {
                return false;
            }

            slug = cn.Trim().ToLowerInvariant();
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
