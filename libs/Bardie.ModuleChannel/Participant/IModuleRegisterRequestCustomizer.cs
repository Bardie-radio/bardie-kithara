using Bardie.ModuleChannel.Manifest;
using Bardie.Modules.V1;

namespace Bardie.ModuleChannel.Participant;

/// <summary>
/// Module- or host-owned overlay that fills <see cref="RegisterRequest"/> kind-specific <c>oneof details</c>
/// (JWKS, search fields, permission ceiling, …). ModuleChannel never interprets those bags.
/// </summary>
public interface IModuleRegisterRequestCustomizer
{
    void Customize(RegisterRequest request, ModuleManifest manifest);
}
