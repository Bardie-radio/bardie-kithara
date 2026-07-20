namespace Kithara.Infrastructure.Persistence.Entities;

public sealed class Tune
{
    public Guid Id { get; set; }
    public string ModuleSlug { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public double? DurationSeconds { get; set; }
    public string? ArtworkUrl { get; set; }
    public string? StorageKey { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? CreatedBy { get; set; }
    public ICollection<QueueEntry> QueueEntries { get; set; } = new List<QueueEntry>();
}

public sealed class QueueEntry
{
    public Guid Id { get; set; }
    public Guid StrunaId { get; set; }
    public Guid TuneId { get; set; }
    public int Position { get; set; }

    public Struna Struna { get; set; } = null!;
    public Tune Tune { get; set; } = null!;
}
