namespace Vigia.Domain.Rules;

/// <summary>
/// What a rule looks for in each result.
/// </summary>
public enum ConditionKind
{
    /// <summary>The result's outcome is down.</summary>
    Down,

    /// <summary>A dimension is above the threshold.</summary>
    Above,

    /// <summary>A dimension is below the threshold.</summary>
    Below,
}
