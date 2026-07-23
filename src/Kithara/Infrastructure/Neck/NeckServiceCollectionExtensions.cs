namespace Kithara.Infrastructure.Neck;

public static class NeckServiceCollectionExtensions
{
    public static IServiceCollection AddKitharaNeck(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<NeckOptions>(options =>
        {
            configuration.GetSection(NeckOptions.SectionName).Bind(options);
            var path = configuration["BARDIE_STRUNA_FIFO_PATH"];
            if (!string.IsNullOrWhiteSpace(path))
            {
                options.StrunaFifoRoot = path.Trim();
            }
        });

        services.AddSingleton<Neck>();
        return services;
    }
}
