using Google.Protobuf.Reflection;

namespace Bardie.ModuleChannel;

/// <summary>
/// Builds full gRPC method paths as seen on <c>ServerCallContext.Method</c>
/// (<c>/package.Service/Method</c>).
/// </summary>
public static class GrpcMethodPath
{
    public static string Format(string package, string service, string method)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        return $"/{package.Trim()}.{service.Trim()}/{method.Trim()}";
    }

    /// <summary>
    /// Path from a generated protobuf <see cref="ServiceDescriptor"/> (stays in sync with <c>package</c> in .proto).
    /// </summary>
    public static string FromDescriptor(ServiceDescriptor service, string methodName)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var method = service.FindMethodByName(methodName.Trim())
            ?? throw new ArgumentException(
                $"Method '{methodName}' not found on service '{service.FullName}'.",
                nameof(methodName));

        return $"/{service.FullName}/{method.Name}";
    }
}
