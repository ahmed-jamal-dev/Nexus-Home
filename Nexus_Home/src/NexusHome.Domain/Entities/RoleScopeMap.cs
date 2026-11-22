namespace NexusHome.Domain.Entities;

/// <summary>
/// Maps platform roles to their granted scopes (OAuth2 space-delimited).
/// SupportAgent is deferred to Phase 1.5 and excluded from MVP.
/// </summary>
public static class RoleScopeMap
{
    private static readonly Dictionary<string, string[]> Map = new()
    {
        ["SuperAdmin"] = new[] { "user:read", "user:manage", "home:read", "home:manage", "platform:health", "platform:audit", "platform:config", "home.provision" },
        ["PlatformAdmin"] = new[] { "user:read", "user:manage", "home:read", "home:manage", "platform:health", "platform:audit", "home.provision" },
        ["Technician"] = new[] { "home.provision", "home:read" }
        // SupportAgent deferred to Phase 1.5
    };

    /// <summary>
    /// Gets the scopes granted by a specific role.
    /// </summary>
    public static IReadOnlyList<string> GetScopes(string roleName)
        => Map.TryGetValue(roleName, out var scopes) ? scopes : Array.Empty<string>();

    /// <summary>
    /// Gets all role-to-scope mappings.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> GetAll() => Map;
}