using Bardie.Modules.V1;

namespace Bardie.ModuleChannel.Manifest;

/// <summary>Builds a core <see cref="RegisterRequest"/> from static manifest identity plus deployment overlays.</summary>
public static class ModuleManifestRegisterExtensions
{
    /// <summary>
    /// Creates a <see cref="RegisterRequest"/> with slug, kind, capabilities, join secret, and advertise address only.
    /// Kind-specific <c>oneof details</c> (auth / source / client) are applied by
    /// <see cref="Participant.IModuleRegisterRequestCustomizer"/> implementations — not by this library.
    /// </summary>
    public static RegisterRequest BuildRegisterRequest(
        this ModuleManifest manifest,
        string joinSecret,
        string advertiseAddress,
        IEnumerable<Participant.IModuleRegisterRequestCustomizer>? customizers = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(joinSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(advertiseAddress);

        ModuleManifestLoader.Validate(manifest);

        var request = new RegisterRequest
        {
            Slug = manifest.Slug,
            JoinSecret = joinSecret,
            Kind = manifest.Kind,
            GrpcAdvertiseAddress = advertiseAddress.Trim(),
        };

        foreach (var capability in manifest.Capabilities)
        {
            if (!string.IsNullOrWhiteSpace(capability))
            {
                request.Capabilities.Add(capability.Trim());
            }
        }

        if (customizers is not null)
        {
            foreach (var customizer in customizers)
            {
                customizer.Customize(request, manifest);
            }
        }

        return request;
    }
}
