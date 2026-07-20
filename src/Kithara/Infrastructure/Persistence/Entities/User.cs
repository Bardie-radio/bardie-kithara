namespace Kithara.Infrastructure.Persistence.Entities;

public enum UserKind
{
    Durable = 0,
    Managed = 1,
    EphemeralGuest = 2,
}

public sealed class User
{
    public Guid Id { get; set; }
    public UserKind Kind { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "active";
    public string? ManagedByModuleSlug { get; set; }
    public Guid? GuestStrunaId { get; set; }

    public ICollection<UserAuthBinding> AuthBindings { get; set; } = new List<UserAuthBinding>();
}

public sealed class UserAuthBinding
{
    public Guid UserId { get; set; }
    public string ProviderSlug { get; set; } = string.Empty;
    public string? ExternalSubject { get; set; }
    public string PayloadJson { get; set; } = "{}";

    public User User { get; set; } = null!;
}
