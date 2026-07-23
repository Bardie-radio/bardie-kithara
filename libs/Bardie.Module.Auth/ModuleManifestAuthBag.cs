using System.Text.Json;
using Bardie.Auth.V1;
using Bardie.Module.Channel.Manifest;

namespace Bardie.Module.Auth;

/// <summary>
/// Parses the opaque <c>auth</c> bag from <see cref="ModuleManifest.Extensions"/>
/// (ModuleChannel does not type kind-specific bags).
/// </summary>
public static class ModuleManifestAuthBag
{
    public const string ExtensionKey = "auth";

    /// <summary>
    /// Builds a <see cref="FormSchemaUi"/> from <c>auth.formFields</c>, or <c>null</c> when absent.
    /// </summary>
    public static FormSchemaUi? TryBuildFormSchema(ModuleManifest manifest)
    {
        var fields = ReadFormFields(manifest);
        if (fields.Count == 0)
        {
            return null;
        }

        var schema = new FormSchemaUi();
        foreach (var field in fields)
        {
            schema.Fields.Add(field);
        }

        return schema;
    }

    /// <summary>
    /// Reads <c>auth.formFields</c> from the manifest. Empty when absent or malformed.
    /// </summary>
    public static IReadOnlyList<FormField> ReadFormFields(ModuleManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (manifest.Extensions is null
            || !manifest.Extensions.TryGetValue(ExtensionKey, out var authElement)
            || authElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        if (!authElement.TryGetProperty("formFields", out var fieldsElement)
            && !authElement.TryGetProperty("form_fields", out fieldsElement))
        {
            return [];
        }

        if (fieldsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var fields = new List<FormField>();
        foreach (var item in fieldsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = ReadString(item, "name") ?? ReadString(item, "Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            fields.Add(new FormField
            {
                Name = name.Trim(),
                Label = (ReadString(item, "label") ?? ReadString(item, "Label") ?? name).Trim(),
                InputType = (ReadString(item, "inputType")
                    ?? ReadString(item, "input_type")
                    ?? ReadString(item, "InputType")
                    ?? "text").Trim(),
                Required = ReadBool(item, "required") ?? ReadBool(item, "Required") ?? false,
            });
        }

        return fields;
    }

    private static string? ReadString(JsonElement obj, string propertyName) =>
        obj.TryGetProperty(propertyName, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    private static bool? ReadBool(JsonElement obj, string propertyName) =>
        obj.TryGetProperty(propertyName, out var el) && (el.ValueKind is JsonValueKind.True or JsonValueKind.False)
            ? el.GetBoolean()
            : null;
}
