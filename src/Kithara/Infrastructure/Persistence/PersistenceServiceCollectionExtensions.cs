using Bardie.Auth.Orchestrator.Ports;
using Bardie.Source.Orchestrator.Ports;
using Kithara.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Kithara.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddKitharaPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = (configuration["DbProvider"] ?? "sqlite").Trim().ToLowerInvariant();
        var connectionString = configuration["DbConnectionString"]
            ?? configuration.GetConnectionString("Kithara")
            ?? "Data Source=kithara.db";

        services.AddDbContextFactory<KitharaDbContext>(options =>
        {
            switch (provider)
            {
                case "postgres":
                case "postgresql":
                    options.UseNpgsql(connectionString);
                    break;
                default:
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        services.AddSingleton<IAuthPersistence, EfAuthPersistence>();
        services.AddSingleton<IBlobStorage, StubBlobStorage>();

        return services;
    }

    public static async Task MigrateKitharaDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<KitharaDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
