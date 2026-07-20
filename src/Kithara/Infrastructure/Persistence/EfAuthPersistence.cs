using Bardie.Auth.Orchestrator.Ports;
using Microsoft.EntityFrameworkCore;

namespace Kithara.Infrastructure.Persistence;

public sealed class EfAuthPersistence : IAuthPersistence
{
    private readonly IDbContextFactory<KitharaDbContext> _dbContextFactory;

    public EfAuthPersistence(IDbContextFactory<KitharaDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        return await db.Users.CountAsync(cancellationToken).ConfigureAwait(false);
    }
}
