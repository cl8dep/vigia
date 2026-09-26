namespace Vigia.Application.Workers;

/// <summary>
/// Everything that defines a worker except its slug. Shared by create and update (full replace).
/// </summary>
public record WorkerSpec
{
    /// <summary>Display name. Defaults to the slug.</summary>
    public string? Name { get; init; }

    /// <summary>Region, mirrored as the <c>vigia:region</c> tag.</summary>
    public string? Region { get; init; }

    /// <summary>User tags (for example <c>network: flystern-vpc</c>). <c>vigia:*</c> is reserved.</summary>
    public IReadOnlyDictionary<string, string?>? Tags { get; init; }
}
