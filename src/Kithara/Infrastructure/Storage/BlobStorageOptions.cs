namespace Kithara.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary><c>local</c> (MVP) \| <c>s3</c> (later).</summary>
    public string Driver { get; set; } = "local";

    /// <summary>Local driver root (<c>BARDIE_STORAGE_PATH</c>).</summary>
    public string Path { get; set; } = "data/blobs";
}
