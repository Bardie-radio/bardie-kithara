using System.Text;
using System.Text.Json;

namespace Kithara.Infrastructure.Persistence;

internal static class BindingPayloadJson
{
    public static string MergeRoles(string payloadJson, IReadOnlyList<string> roles)
    {
        Dictionary<string, JsonElement> properties;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
            properties = doc.RootElement.EnumerateObject()
                .Where(p => p.Name != "roles")
                .ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch (JsonException)
        {
            return payloadJson;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var (key, value) in properties)
            {
                writer.WritePropertyName(key);
                value.WriteTo(writer);
            }

            writer.WritePropertyName("roles");
            writer.WriteStartArray();
            foreach (var role in roles)
            {
                writer.WriteStringValue(role);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
