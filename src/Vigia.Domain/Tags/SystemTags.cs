namespace Vigia.Domain.Tags;

/// <summary>
/// Tags in the reserved <c>vigia:</c> namespace. Derived by the system from facts on an entity; never set by users.
/// </summary>
public static class SystemTags
{
    /// <summary>Reserved namespace prefix.</summary>
    public const string Prefix = "vigia:";

    /// <summary>On checks: the plugin id.</summary>
    public const string Plugin = "vigia:plugin";

    /// <summary>On workers: present on the built-in worker (flag, no value).</summary>
    public const string Builtin = "vigia:builtin";

    /// <summary>On workers: the configured region.</summary>
    public const string Region = "vigia:region";

    /// <summary>Every known system tag. Selectors may reference these; nothing else in the namespace is valid.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal) { Plugin, Builtin, Region };

    /// <summary>Whether <paramref name="key"/> is in the reserved namespace.</summary>
    public static bool IsReserved(string key)
    {
        return key.StartsWith(Prefix, StringComparison.Ordinal) || key == Prefix.TrimEnd(':');
    }
}
