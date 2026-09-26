namespace Vigia.Infrastructure.Results;

/// <summary>
/// Result rollup and retention settings, bound from the <c>Retention</c> config section.
/// </summary>
public sealed class RetentionOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Retention";

    /// <summary>Run the rollup and retention job in this process.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often the job runs.</summary>
    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How long raw results are kept.</summary>
    public TimeSpan RawResults { get; set; } = TimeSpan.FromDays(14);

    /// <summary>How long hourly rollups are kept.</summary>
    public TimeSpan Rollups { get; set; } = TimeSpan.FromDays(400);

    /// <summary>
    /// Hours before the latest rollup that are recomputed on every run, so results that arrive late
    /// (remote agents uploading after degraded mode) still land in their hour.
    /// </summary>
    public TimeSpan RollupLookback { get; set; } = TimeSpan.FromHours(48);

    /// <summary>Rows deleted per statement, to keep locks and transaction size small.</summary>
    public int DeleteBatchSize { get; set; } = 5000;
}
