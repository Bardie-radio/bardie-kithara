namespace Kithara.Infrastructure.Persistence.Entities;

/// <summary>
/// Principal-scoped source-search result refs (play/queue by ref until TTL or next search).
/// Not a Tune relationship — Feature Search owns the behaviour.
/// </summary>
public sealed class SearchResultCacheEntry
{
    public Guid Id { get; set; }
    public Guid PrincipalUserId { get; set; }
    public string ModuleSlug { get; set; } = string.Empty;
    public string QueryKey { get; set; } = string.Empty;
    public string ResultsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public User Principal { get; set; } = null!;
}
