using Bardie.ModuleChannel.Certificates;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Bardie.ModuleChannel.Hosting;

/// <summary>
/// Applies module mTLS identity on every gRPC call shape. Methods listed in
/// <see cref="ModuleChannelOptions.AllowWithoutClientCertificate"/> may proceed without a client cert
/// (optional cert still recorded when present). All other RPCs require a CA-validated client certificate
/// when <see cref="ModuleChannelOptions.UseMtls"/> is true.
/// Validated slug is stored in <see cref="ServerCallContext.UserState"/> under <see cref="ModuleSlugUserStateKey"/>.
/// </summary>
public sealed class ModuleChannelBootstrapInterceptor : Interceptor
{
    public const string ModuleSlugUserStateKey = "bardie.module.slug";

    private readonly IModuleCertificateValidator _validator;
    private readonly ModuleChannelOptions _options;
    private readonly HashSet<string> _allowWithoutClientCertificate;

    public ModuleChannelBootstrapInterceptor(
        IModuleCertificateValidator validator,
        Microsoft.Extensions.Options.IOptions<ModuleChannelOptions> options)
    {
        _validator = validator;
        _options = options.Value;
        _allowWithoutClientCertificate = new HashSet<string>(
            _options.AllowWithoutClientCertificate ?? [],
            StringComparer.Ordinal);
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ApplyModuleIdentity(context);
        return continuation(request, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ApplyModuleIdentity(context);
        return continuation(requestStream, context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ApplyModuleIdentity(context);
        return continuation(request, responseStream, context);
    }

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ApplyModuleIdentity(context);
        return continuation(requestStream, responseStream, context);
    }

    private void ApplyModuleIdentity(ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var clientCert = httpContext.Connection.ClientCertificate;
        var allowWithoutCert = _allowWithoutClientCertificate.Contains(context.Method);

        if (allowWithoutCert)
        {
            if (clientCert is not null && _validator.TryValidate(clientCert, out var presentedSlug))
            {
                context.UserState[ModuleSlugUserStateKey] = presentedSlug;
            }

            return;
        }

        if (!_options.UseMtls)
        {
            return;
        }

        if (!_validator.TryValidate(clientCert, out var slug))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Valid module client certificate required."));
        }

        context.UserState[ModuleSlugUserStateKey] = slug;
    }
}
