namespace Vigia.Application.Rules;

/// <summary>
/// Rule evaluation settings, bound from the <c>Alerting</c> config section.
/// </summary>
public sealed class AlertingOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Alerting";

    /// <summary>Quorum for checks that do not set one: <c>majority</c>, a count, or a percentage.</summary>
    public string DefaultQuorum { get; set; } = "majority";

    /// <summary>How often every service's health is recomputed, to repair drift and apply time-based inputs.</summary>
    public TimeSpan HealthReconcileInterval { get; set; } = TimeSpan.FromMinutes(1);
}
