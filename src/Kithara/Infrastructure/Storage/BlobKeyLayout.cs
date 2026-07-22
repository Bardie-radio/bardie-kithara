namespace Kithara.Infrastructure.Storage;

/// <summary>
/// Opaque library blob keys under <c>tunes/&lt;source_slug&gt;/…</c>.
/// </summary>
public static class BlobKeyLayout
{
    public const string TunesSegment = "tunes";

    public static string AssignKey(string sourceSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSlug);
        var slug = sourceSlug.Trim().ToLowerInvariant();
        return $"{TunesSegment}/{slug}/{Guid.NewGuid():N}";
    }

    public static string ModulePrefix(string sourceSlug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSlug);
        var slug = sourceSlug.Trim().ToLowerInvariant();
        return $"{TunesSegment}/{slug}/";
    }

    /// <summary>
    /// Rejects empty keys, traversal, absolute paths, and keys outside <c>tunes/&lt;slug&gt;/…</c>.
    /// </summary>
    public static void EnsureValidKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.Contains('\\')
            || key.Contains('\0')
            || key.StartsWith('/')
            || key.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Blob key must be a relative opaque path without traversal.", nameof(key));
        }

        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 3
            || !string.Equals(segments[0], TunesSegment, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(segments[1])
            || string.IsNullOrWhiteSpace(segments[2]))
        {
            throw new ArgumentException(
                "Blob key must be under tunes/<source_slug>/… (at least one object segment).",
                nameof(key));
        }
    }

    /// <summary>
    /// Ensures <paramref name="key"/> is valid and owned by <paramref name="sourceSlug"/>.
    /// </summary>
    public static void EnsureKeyOwnedBy(string key, string sourceSlug)
    {
        EnsureValidKey(key);
        var prefix = ModulePrefix(sourceSlug);
        if (!key.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                $"Blob key must stay under '{prefix}' for module '{sourceSlug}'.");
        }
    }

    /// <summary>
    /// Maps an opaque key to a filesystem path under <paramref name="storageRoot"/>, rejecting escape.
    /// </summary>
    public static string ResolvePath(string storageRoot, string key)
    {
        EnsureValidKey(key);

        var rootFull = Path.GetFullPath(storageRoot);
        if (!rootFull.EndsWith(Path.DirectorySeparatorChar)
            && !rootFull.EndsWith(Path.AltDirectorySeparatorChar))
        {
            rootFull += Path.DirectorySeparatorChar;
        }

        var relative = key.Replace('/', Path.DirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(storageRoot, relative));
        if (!candidate.StartsWith(rootFull, StringComparison.Ordinal)
            && !string.Equals(
                candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(storageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.Ordinal))
        {
            throw new ArgumentException("Blob key resolves outside the storage root.", nameof(key));
        }

        return candidate;
    }
}
