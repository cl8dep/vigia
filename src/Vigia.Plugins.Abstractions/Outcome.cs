namespace Vigia.Plugins;

/// <summary>
/// Result of a single probe.
/// </summary>
public enum Outcome
{
    /// <summary>The target behaved as expected.</summary>
    Up,

    /// <summary>The target failed the check.</summary>
    Down,

    /// <summary>The probe itself could not run (bad config, agent problem). Says nothing about the target.</summary>
    Error,
}
