using Microsoft.EntityFrameworkCore;

public class KitharaDbContext : DbContext
{
    public DbSet<Tune> Tunes { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<Struna> Strunas { get; set; }

    public KitharaDbContext(DbContextOptions<KitharaDbContext> options) : base(options) { }
}