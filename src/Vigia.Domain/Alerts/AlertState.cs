namespace Vigia.Domain.Alerts;

/// <summary>
/// Lifecycle of an alert.
/// </summary>
public enum AlertState
{
    /// <summary>The rule's condition holds.</summary>
    Firing,

    /// <summary>The condition stopped holding for the rule's recovery count.</summary>
    Resolved,
}
