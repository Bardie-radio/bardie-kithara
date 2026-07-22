using Bardie.Module.Channel.Manifest;
using Bardie.Module.Channel.Participant;
using Bardie.Modules.V1;

namespace Bardie.Module.Auth;

/// <summary>Attaches runtime JWKS on Module Registry Register.</summary>
public sealed class AuthJwksRegisterRequestCustomizer : IModuleRegisterRequestCustomizer
{
    private readonly AuthModuleJwtService _tokens;

    public AuthJwksRegisterRequestCustomizer(AuthModuleJwtService tokens)
    {
        _tokens = tokens;
    }

    public void Customize(RegisterRequest request, ModuleManifest manifest)
    {
        request.Auth = new AuthRegisterDetails
        {
            JwksJson = _tokens.ExportJwksJson(),
        };
    }
}
