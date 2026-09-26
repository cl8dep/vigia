using Vigia.Domain.Rules;

namespace Vigia.Domain.Alerts;

/// <summary>
/// One firing (or past) occurrence of a rule on a check. At most one firing alert exists per rule and check;
/// while it fires, new matching results update it instead of opening another.
/// </summary>
public sealed class Alert
{
    private Alert()
    {
    }

    /// <summary>Opens a firing alert.</summary>
    public Alert(Guid ruleId, Guid checkId, Severity severity, string message, DateTimeOffset at)
    {
        Id = Guid.CreateVersion7(at);
        RuleId = ruleId;
        CheckId = checkId;
        Severity = severity;
        State = AlertState.Firing;
        Message = message;
        FiredAt = at;
        LastSeenAt = at;
        Occurrences = 1;
    }

    public Guid Id { get; private set; }

    /// <summary>Rule that fired.</summary>
    public Guid RuleId { get; private set; }

    /// <summary>Check the rule fired on.</summary>
    public Guid CheckId { get; private set; }

    /// <summary>Severity copied from the rule when the alert opened.</summary>
    public Severity Severity { get; private set; }

    /// <summary>Firing or resolved.</summary>
    public AlertState State { get; private set; }

    /// <summary>Latest description of the problem. Updates while firing; never opens a new alert by itself.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>When the alert opened.</summary>
    public DateTimeOffset FiredAt { get; private set; }

    /// <summary>Last matching result while firing.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>When it resolved, if it did.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Matching results while firing, including the one that opened it.</summary>
    public long Occurrences { get; private set; }

    /// <summary>Records another matching result while firing.</summary>
    public void Seen(string message, DateTimeOffset at)
    {
        Message = message;
        LastSeenAt = at;
        Occurrences++;
    }

    /// <summary>Closes the alert.</summary>
    public void Resolve(DateTimeOffset at)
    {
        State = AlertState.Resolved;
        ResolvedAt = at;
    }
}
