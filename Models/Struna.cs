/// <summary>
/// Represent model for audio stream of the radio service.
/// </summary>
/// <remarks>
/// Named after ukrainian word for "string".
/// </remarks>
public class Struna
{

    /// <summary>
    /// Gets or sets the unique identifier for the radio stream.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the radio stream.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the radio stream.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the radio stream.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image URL for the radio stream.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
    
}