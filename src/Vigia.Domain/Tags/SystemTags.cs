namespace Vigia.Domain.Tags;

/// <summary>
/// The catalog of tags in the reserved <c>vigia:</c> namespace: the single place that says which exist, who assigns
/// them and what values they take. Same model as Piro's RFC 0008.
/// </summary>
public static class SystemTags
{
    /// <summary>Reserved namespace prefix.</summary>
    public const string Prefix = "vigia:";

    /// <summary>On checks: the plugin id.</summary>
    public const string Plugin = "vigia:plugin";

    /// <summary>On workers: present on the built-in worker.</summary>
    public const string Builtin = "vigia:builtin";

    /// <summary>On workers: where the worker runs (not where a target is deployed; that is the user tag <c>region</c>).</summary>
    public const string Region = "vigia:region";

    /// <summary>On services and checks: a third-party provider (Sabre, Stripe, a DNS provider). Set by users.</summary>
    public const string External = "vigia:external";

    /// <summary>Every system tag.</summary>
    public static readonly IReadOnlyList<SystemTagDefinition> All =
    [
        new(Plugin, TagAssignment.Reconciled, TagValueKind.Value, new HashSet<TaggedEntity> { TaggedEntity.Check }),
        new(Builtin, TagAssignment.Reconciled, TagValueKind.Flag, new HashSet<TaggedEntity> { TaggedEntity.Worker }),
        new(Region, TagAssignment.Reconciled, TagValueKind.Value, new HashSet<TaggedEntity> { TaggedEntity.Worker }),
        new(External, TagAssignment.Assignable, TagValueKind.Flag, new HashSet<TaggedEntity> { TaggedEntity.Service, TaggedEntity.Check }),
    ];

    /// <summary>Whether <paramref name="key"/> is in the reserved namespace.</summary>
    public static bool IsReserved(string key)
    {
        return key.StartsWith(Prefix, StringComparison.Ordinal) || key == Prefix.TrimEnd(':');
    }

    /// <summary>The definition of a system tag, or null when the key is not in the catalog.</summary>
    public static SystemTagDefinition? Find(string key)
    {
        return All.FirstOrDefault(d => d.Key == key);
    }

    /// <summary>Whether the system derives this key; such tags survive a user tag replace.</summary>
    public static bool IsReconciled(string key)
    {
        return Find(key)?.Assignment == TagAssignment.Reconciled;
    }
}
