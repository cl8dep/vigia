namespace Vigia.Plugins;

/// <summary>
/// Static description of a check plugin, read by the host at load time.
/// </summary>
public sealed class CheckManifest
{
    /// <summary>Human-readable name shown in the UI.</summary>
    public required string Label { get; init; }

    /// <summary>One or two sentences on what the check verifies.</summary>
    public required string Description { get; init; }

    /// <summary>Config record type. The host builds the config schema from its properties and attributes.</summary>
    public required Type ConfigType { get; init; }

    /// <summary>Interval used when a check does not set one.</summary>
    public TimeSpan DefaultInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Measurements this check produces. Alert rules can only target declared dimensions.</summary>
    public IReadOnlyList<DimensionSpec> Dimensions { get; init; } = [];
}
