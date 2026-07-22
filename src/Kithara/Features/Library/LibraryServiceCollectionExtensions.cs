namespace Kithara.Features.Library;

public static class LibraryServiceCollectionExtensions
{
    public static IServiceCollection AddKitharaLibrary(this IServiceCollection services)
    {
        services.AddSingleton<TuneLibrary>();
        services.AddSingleton<LibraryService>();
        return services;
    }
}
