namespace Vigia.Domain.Results;

/// <summary>
/// Outcome of a stored probe result.
/// </summary>
public enum ResultOutcome
{
    /// <summary>The target behaved as expected.</summary>
    Up,

    /// <summary>The target failed the check.</summary>
    Down,

    /// <summary>The probe could not run. Says nothing about the target.</summary>
    Error,
}
