using Bardie.Module.Channel.Participant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bardie.Module.Auth;

public static class AuthModuleServiceCollectionExtensions
{
    /// <summary>
    /// Registers JWT options, <see cref="AuthModuleJwtService"/>, and JWKS Register customizer.
    /// </summary>
    public static IServiceCollection AddAuthModuleJwt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AuthModuleJwtOptions>(configuration.GetSection(AuthModuleJwtOptions.SectionName));
        services.AddSingleton<AuthModuleJwtService>();
        services.AddSingleton<IModuleRegisterRequestCustomizer, AuthJwksRegisterRequestCustomizer>();
        return services;
    }
}
