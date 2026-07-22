using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bardie.Module.Channel.Manifest;

/// <summary>
/// Static module identity shipped as <c>module.manifest.json</c>.
/// Kind-specific Register oneof bags (auth JWKS, source search fields, client ceiling, …)
/// are <b>not</b> modeled here — modules apply them via <see cref="Participant.IModuleRegisterRequestCustomizer"/>.
/// Extra JSON properties are retained in <see cref="Extensions"/> for module-local parsing.
/// </summary>
public sealed class ModuleManifest
{
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("otelServiceName")]
    public string? OtelServiceName { get; set; }

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];

    /// <summary>
    /// Opaque leftover JSON (e.g. module-local <c>source</c> / <c>client</c> bags).
    /// ModuleChannel never interprets these keys.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
