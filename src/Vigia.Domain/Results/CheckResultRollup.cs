namespace Vigia.Domain.Results;

/// <summary>
/// Hourly summary of one check's results from one worker. Kept much longer than raw results.
/// Written by the rollup job only.
/// </summary>
public sealed class CheckResultRollup
{
    private CheckResultRollup()
    {
    }

    /// <summary>Check the results belong to.</summary>
    public Guid CheckId { get; private set; }

    /// <summary>Worker that produced the results.</summary>
    public string Worker { get; private set; } = string.Empty;

    /// <summary>Start of the hour (UTC).</summary>
    public DateTimeOffset HourStart { get; private set; }

    /// <summary>Probes that were up.</summary>
    public long Up { get; private set; }

    /// <summary>Probes that were down.</summary>
    public long Down { get; private set; }

    /// <summary>Probes that could not run.</summary>
    public long Error { get; private set; }

    /// <summary>Per-dimension statistics, for whatever dimensions the plugin reported.</summary>
    public Dictionary<string, DimensionStats> Dimensions { get; private set; } = [];
}
