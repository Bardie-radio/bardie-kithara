/// <summary>
/// Represents a model for a audio file in the radio service.
/// </summary>
public class Tune
{
    /// <summary>
    /// Gets or sets the unique identifier for the audio file.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the file name of the audio file.
    /// </summary>
    public string FIleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the audio file.
    /// </summary>
    /// <remarks>
    /// Needed for deduplication of audio files.
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist of the audio file.
    /// </summary>
    /// <remarks>
    /// Needed for deduplication of audio files.
    /// </remarks>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets playlists associated with the audio file.
    /// </summary>
    public List<Playlist> Playlists { get; set; } = new();
}