namespace Vigia.Domain.Health;

/// <summary>
/// Stored snapshot of a service's derived health. Written only by the health updater, which recomputes it from alerts
/// and dependencies; never edited by hand.
/// </summary>
public sealed class ServiceHealth
{
    private ServiceHealth()
    {
    }

    /// <summary>Creates the first snapshot of a service.</summary>
    public ServiceHealth(Guid serviceId, HealthState state, string reason, IReadOnlyDictionary<string, HealthState> partitions, DateTimeOffset at)
    {
        ServiceId = serviceId;
        State = state;
        Reason = reason;
        Partitions = new Dictionary<string, HealthState>(partitions);
        Since = at;
        EvaluatedAt = at;
    }

    /// <summary>Service this snapshot belongs to.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Current state.</summary>
    public HealthState State { get; private set; }

    /// <summary>Why the service is in this state, for people.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>State per partition, when the service is partitioned.</summary>
    public Dictionary<string, HealthState> Partitions { get; private set; } = [];

    /// <summary>When the current state started.</summary>
    public DateTimeOffset Since { get; private set; }

    /// <summary>Last recomputation.</summary>
    public DateTimeOffset EvaluatedAt { get; private set; }

    /// <summary>Applies a recomputation. Returns true when the state changed.</summary>
    public bool Update(HealthState state, string reason, IReadOnlyDictionary<string, HealthState> partitions, DateTimeOffset at)
    {
        var changed = state != State;
        if (changed)
        {
            Since = at;
        }

        State = state;
        Reason = reason;
        Partitions = new Dictionary<string, HealthState>(partitions);
        EvaluatedAt = at;
        return changed;
    }
}
