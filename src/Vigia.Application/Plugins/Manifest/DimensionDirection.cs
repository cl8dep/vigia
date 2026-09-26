namespace Vigia.Application.Plugins.Manifest;

/// <summary>
/// Which way a dimension gets worse. Lets alert rules and the UI reason about thresholds generically.
/// </summary>
public enum DimensionDirection
{
    /// <summary>Bigger values are worse, for example latency.</summary>
    HigherIsWorse,

    /// <summary>Smaller values are worse, for example days to certificate expiry.</summary>
    LowerIsWorse,
}
