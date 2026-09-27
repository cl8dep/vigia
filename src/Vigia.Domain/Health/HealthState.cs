namespace Vigia.Domain.Health;

/// <summary>
/// Health of a check or service. The numeric order is severity: higher is worse, and combining states keeps the worst.
/// </summary>
public enum HealthState
{
    /// <summary>Nothing to judge: no checks, or none that can run.</summary>
    Unknown = 0,

    /// <summary>Everything fine.</summary>
    Operational = 1,

    /// <summary>In a maintenance window (reserved; maintenance windows come later).</summary>
    Maintenance = 2,

    /// <summary>Working with problems.</summary>
    Degraded = 3,

    /// <summary>Down in some partitions (for example one region of several).</summary>
    PartialOutage = 4,

    /// <summary>Not working.</summary>
    Down = 5,
}
