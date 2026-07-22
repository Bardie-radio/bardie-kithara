using Kithara.Features.Library;
using Kithara.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kithara.Tests;

public class LibraryEnsureTuneTests
{
    [Fact]
    public async Task EnsureTune_creates_then_updates_by_module_and_external_id()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "kithara-tunes-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContextFactory<KitharaDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            await using var provider = services.BuildServiceProvider();

            var factory = provider.GetRequiredService<IDbContextFactory<KitharaDbContext>>();
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
            }

            var library = new TuneLibrary(factory);

            var created = await library.EnsureTuneAsync(new EnsureTuneCommand(
                ModuleSlug: "magpie",
                ExternalId: "yt-abc",
                Title: "First",
                Artist: null,
                DurationSeconds: null,
                ArtworkUrl: null,
                StorageKey: "tunes/magpie/obj1",
                ContentType: null,
                SizeBytes: 12));

            Assert.True(created.Created);

            var updated = await library.EnsureTuneAsync(new EnsureTuneCommand(
                ModuleSlug: "magpie",
                ExternalId: "yt-abc",
                Title: "Second",
                Artist: "Artist",
                DurationSeconds: null,
                ArtworkUrl: null,
                StorageKey: "tunes/magpie/obj2",
                ContentType: null,
                SizeBytes: 99));

            Assert.False(updated.Created);
            Assert.Equal(created.TuneId, updated.TuneId);

            await using var verify = await factory.CreateDbContextAsync();
            var row = await verify.Tunes.SingleAsync();
            Assert.Equal("Second", row.Title);
            Assert.Equal("Artist", row.Artist);
            Assert.Equal("tunes/magpie/obj2", row.StorageKey);
            Assert.Equal(99, row.SizeBytes);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }
}
