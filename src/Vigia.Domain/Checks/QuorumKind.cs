namespace Vigia.Domain.Checks;

/// <summary>
/// How a <see cref="Quorum"/> counts workers.
/// </summary>
public enum QuorumKind
{
    /// <summary>A fixed number of workers.</summary>
    Count,

    /// <summary>A percentage of the workers with fresh results, rounded up.</summary>
    Percent,

    /// <summary>More than half of the workers with fresh results.</summary>
    Majority,
}
