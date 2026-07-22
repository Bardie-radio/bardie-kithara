using Bardie.ModuleChannel.Certificates;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Bardie.ModuleChannel.Hosting;

/// <summary>
/// Allows Module Registry <c>Register</c> without a client certificate; requires a CA-validated client cert for other RPCs.
/// Validated slug is stored in <see cref="ServerCallContext.UserState"/> under <see cref="ModuleSlugUserStateKey"/>.
/// </summary>
public sealed class ModuleChannelBootstrapInterceptor : Interceptor
{
    public const string ModuleSlugUserStateKey = "bardie.module.slug";

    private readonly IModuleCertificateValidator _validator;
    private readonly ModuleChannelOptions _options;

    public ModuleChannelBootstrapInterceptor(
        IModuleCertificateValidator validator,
        Microsoft.Extensions.Options.IOptions<ModuleChannelOptions> options)
    {
        _validator = validator;
        _options = options.Value;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var httpContext = context.GetHttpContext();
        var isRegister = IsRegisterMethod(context.Method);
        var clientCert = httpContext.Connection.ClientCertificate;

        if (isRegister)
        {
            if (clientCert is not null && _validator.TryValidate(clientCert, out var presentedSlug))
            {
                context.UserState[ModuleSlugUserStateKey] = presentedSlug;
            }

            return await continuation(request, context).ConfigureAwait(false);
        }

        if (!_options.UseMtls)
        {
            return await continuation(request, context).ConfigureAwait(false);
        }

        if (!_validator.TryValidate(clientCert, out var slug))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Valid module client certificate required."));
        }

        context.UserState[ModuleSlugUserStateKey] = slug;
        return await continuation(request, context).ConfigureAwait(false);
    }

    private static bool IsRegisterMethod(string fullMethodName) =>
        fullMethodName.EndsWith("/Register", StringComparison.Ordinal);
}
