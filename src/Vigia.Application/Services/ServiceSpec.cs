using System.Text.Json;

namespace Vigia.Application.Services;

/// <summary>
/// Everything that defines a service except its slug. Shared by create and update (full replace).
/// </summary>
public record ServiceSpec
{
    /// <summary>Display name. Defaults to the slug.</summary>
    public string? Name { get; init; }

    /// <summary>Tags, including assignable system tags such as <c>vigia:external</c>.</summary>
    public IReadOnlyDictionary<string, string?>? Tags { get; init; }

    /// <summary>Tag selector over checks; omitted means the service has no checks (never "all checks").</summary>
    public IReadOnlyDictionary<string, JsonElement>? Checks { get; init; }

    /// <summary>Check tag key that partitions the service, usually <c>region</c>.</summary>
    public string? PartitionBy { get; init; }

    /// <summary>Services this one depends on.</summary>
    public IReadOnlyList<DependencySpec>? DependsOn { get; init; }
}
