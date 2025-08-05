
internal class Playlist
{
    /// <summary>
    /// Gets or sets the unique identifier for the playlist.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of tunes in the playlist.
    /// </summary>
    public List<Tune> Tunes { get; set; } = new();
}