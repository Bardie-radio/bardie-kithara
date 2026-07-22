using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Bardie.Module.Channel.Manifest;

/// <summary>Loads and overlays <see cref="ModuleManifest"/> from file / stream / configuration.</summary>
public static class ModuleManifestLoader
{
    public const string DefaultFileName = "module.manifest.json";
    public const string SlugOverrideEnvironmentVariable = "MODULE_SLUG_OVERRIDE";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ModuleManifest LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Module manifest not found at '{path}'.", path);
        }

        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static ModuleManifest Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var manifest = JsonSerializer.Deserialize<ModuleManifest>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Module manifest JSON deserialized to null.");
        Validate(manifest);
        return manifest;
    }

    public static ModuleManifest LoadFromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var manifest = JsonSerializer.Deserialize<ModuleManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Module manifest JSON deserialized to null.");
        Validate(manifest);
        return manifest;
    }

    /// <summary>
    /// Applies env overlays (today: <c>MODULE_SLUG_OVERRIDE</c>). Does not mutate join secret or advertise address.
    /// </summary>
    public static ModuleManifest ApplyEnvironmentOverlays(
        ModuleManifest manifest,
        IConfiguration? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var overrideSlug = configuration?[SlugOverrideEnvironmentVariable]
            ?? Environment.GetEnvironmentVariable(SlugOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideSlug))
        {
            manifest.Slug = overrideSlug.Trim().ToLowerInvariant();
        }

        Validate(manifest);
        return manifest;
    }

    /// <summary>
    /// Resolves a manifest path: explicit path, else <c>MODULE_MANIFEST_PATH</c>, else <paramref name="contentRoot"/>/<c>module.manifest.json</c>.
    /// </summary>
    public static string ResolvePath(string? explicitPath, string? contentRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        var fromEnv = Environment.GetEnvironmentVariable("MODULE_MANIFEST_PATH");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var root = contentRoot ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, DefaultFileName);
    }

    public static void Validate(ModuleManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (string.IsNullOrWhiteSpace(manifest.Slug))
        {
            throw new InvalidOperationException("Module manifest requires a non-empty 'slug'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Kind))
        {
            throw new InvalidOperationException("Module manifest requires a non-empty 'kind'.");
        }

        manifest.Slug = manifest.Slug.Trim().ToLowerInvariant();
        manifest.Kind = manifest.Kind.Trim().ToLowerInvariant();
        manifest.Capabilities ??= [];
    }
}
