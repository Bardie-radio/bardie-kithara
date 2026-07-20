using System.Text.Json;

namespace Kithara.Features.Modules;

public sealed class ModuleRegistryOptions
{
    public const string SectionName = "ModuleRegistry";

    /// <summary>Default heartbeat TTL before a module is considered expired.</summary>
    public TimeSpan HeartbeatTtl { get; set; } = TimeSpan.FromSeconds(90);

    /// <summary>Suggested client heartbeat interval returned on Heartbeat.</summary>
    public int NextHeartbeatAfterSeconds { get; set; } = 30;

    /// <summary>Slug → join secret. Populated from <c>BARDIE_JOIN_SECRETS</c> JSON.</summary>
    public Dictionary<string, string> JoinSecrets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class JoinSecretsConfiguration
{
    public static Dictionary<string, string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
        return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    public static bool Validate(IReadOnlyDictionary<string, string> secrets, string slug, string provided)
    {
        if (!secrets.TryGetValue(slug, out var expected))
        {
            return false;
        }

        return CryptographicEquals(expected, provided);
    }

    private static bool CryptographicEquals(string expected, string provided)
    {
        var a = System.Text.Encoding.UTF8.GetBytes(expected);
        var b = System.Text.Encoding.UTF8.GetBytes(provided);
        if (a.Length != b.Length)
        {
            // Still compare to reduce timing noise on length mismatch.
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, a);
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
    }
}
