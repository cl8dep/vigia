namespace Vigia.Domain.Health;

/// <summary>
/// One transition in a service's health history. Append-only.
/// </summary>
public sealed class ServiceHealthChange
{
    private ServiceHealthChange()
    {
    }

    /// <summary>Records a transition.</summary>
    public ServiceHealthChange(Guid serviceId, HealthState? from, HealthState to, string reason, DateTimeOffset at)
    {
        Id = Guid.CreateVersion7(at);
        ServiceId = serviceId;
        From = from;
        To = to;
        Reason = reason;
        At = at;
    }

    public Guid Id { get; private set; }

    /// <summary>Service that changed.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Previous state, or null for the first recorded state.</summary>
    public HealthState? From { get; private set; }

    /// <summary>New state.</summary>
    public HealthState To { get; private set; }

    /// <summary>Why, at the time of the change.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>When it changed.</summary>
    public DateTimeOffset At { get; private set; }
}
