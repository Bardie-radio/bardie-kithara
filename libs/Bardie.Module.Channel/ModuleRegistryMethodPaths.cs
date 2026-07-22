using Bardie.Modules.V1;

namespace Bardie.Module.Channel;

/// <summary>
/// Full method paths for Module Registry RPCs, derived from the generated
/// <see cref="ModuleRegistry.Descriptor"/> (proto <c>package</c> + service name — do not hardcode).
/// </summary>
public static class ModuleRegistryMethodPaths
{
    public static string Register { get; } =
        GrpcMethodPath.FromDescriptor(ModuleRegistry.Descriptor, nameof(ModuleRegistry.ModuleRegistryBase.Register));

    public static string Heartbeat { get; } =
        GrpcMethodPath.FromDescriptor(ModuleRegistry.Descriptor, nameof(ModuleRegistry.ModuleRegistryBase.Heartbeat));
}
