using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
/// <summary>
/// Represents a model for an audio file in the radio service.
/// </summary>
[Table("Tunes")]
public class Tune
{
    [Key]
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty; // Path to audio file

    public string Artist { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public string AlbumArtist { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string CoverArtUrl { get; set; } = string.Empty;

    public int Year { get; set; }

    public Guid PlaylistId { get; set; }
    [ForeignKey("PlaylistId")]
    public Playlist? Playlist { get; set; }

    /// <summary>
    /// Gets or sets playlists associated with the audio file.
    /// </summary>
    public List<Playlist> Playlists { get; set; } = new();
}