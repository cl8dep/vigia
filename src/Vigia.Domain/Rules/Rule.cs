using Vigia.Domain.Common;

namespace Vigia.Domain.Rules;

/// <summary>
/// Turns results into alerts: a condition that must hold for <see cref="For"/> consecutive results to fire,
/// and fail to hold for <see cref="RecoverAfter"/> consecutive results to resolve.
/// </summary>
/// <remarks>
/// A rule targets one check or every check matching a label selector. Rules are additive: a check matched by
/// several rules gets one alert per rule.
/// </remarks>
public sealed class Rule : Entity
{
    private Rule()
    {
    }

    /// <summary>Creates a rule.</summary>
    /// <exception cref="DomainException">The target or condition is invalid.</exception>
    public Rule(string slug, string name, ManagedBy managedBy)
        : base(slug, name, managedBy)
    {
        Enabled = true;
    }

    /// <summary>Check this rule targets, or null when it uses <see cref="Selector"/>.</summary>
    public Guid? CheckId { get; private set; }

    /// <summary>Labels a check must all have to be targeted. The key <c>plugin</c> matches the check's plugin id.</summary>
    public Dictionary<string, string> Selector { get; private set; } = [];

    /// <summary>What the rule looks for.</summary>
    public ConditionKind Condition { get; private set; }

    /// <summary>Dimension compared for <see cref="ConditionKind.Above"/> and <see cref="ConditionKind.Below"/>.</summary>
    public string? Dimension { get; private set; }

    /// <summary>Threshold for <see cref="ConditionKind.Above"/> and <see cref="ConditionKind.Below"/>.</summary>
    public double? Threshold { get; private set; }

    /// <summary>Consecutive matching results needed to fire.</summary>
    public int For { get; private set; } = 1;

    /// <summary>Consecutive non-matching results needed to resolve.</summary>
    public int RecoverAfter { get; private set; } = 1;

    /// <summary>Severity of the alerts this rule creates.</summary>
    public Severity Severity { get; private set; }

    /// <summary>Disabled rules are not evaluated. Their firing alerts stay until resolved by hand.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Points the rule at one check.</summary>
    public void TargetCheck(Guid checkId)
    {
        CheckId = checkId;
        Selector = [];
    }

    /// <summary>Points the rule at every check matching <paramref name="selector"/>.</summary>
    /// <exception cref="DomainException">The selector is empty.</exception>
    public void TargetSelector(IReadOnlyDictionary<string, string> selector)
    {
        if (selector.Count == 0)
        {
            throw new DomainException("A selector needs at least one label.");
        }

        CheckId = null;
        Selector = new Dictionary<string, string>(selector);
    }

    /// <summary>Sets the condition and thresholds.</summary>
    /// <exception cref="DomainException">The combination is invalid.</exception>
    public void Configure(ConditionKind condition, string? dimension, double? threshold, int @for, int recoverAfter, Severity severity)
    {
        if (condition == ConditionKind.Down && (dimension is not null || threshold is not null))
        {
            throw new DomainException("An outcome condition takes no dimension or threshold.");
        }

        if (condition != ConditionKind.Down && (string.IsNullOrWhiteSpace(dimension) || threshold is null))
        {
            throw new DomainException("A threshold condition needs a dimension and a threshold.");
        }

        if (@for < 1 || recoverAfter < 1)
        {
            throw new DomainException("'for' and 'recoverAfter' must be at least 1.");
        }

        Condition = condition;
        Dimension = dimension;
        Threshold = threshold;
        For = @for;
        RecoverAfter = recoverAfter;
        Severity = severity;
    }

    /// <summary>Whether this rule applies to a check with the given plugin and labels.</summary>
    public bool Targets(Guid checkId, string plugin, IReadOnlyDictionary<string, string> labels)
    {
        if (CheckId is not null)
        {
            return CheckId == checkId;
        }

        return Selector.Count > 0 && Selector.All(s => s.Key == "plugin" ? s.Value == plugin : labels.TryGetValue(s.Key, out var v) && v == s.Value);
    }

    /// <summary>Starts evaluating the rule.</summary>
    public void Enable()
    {
        Enabled = true;
    }

    /// <summary>Stops evaluating the rule.</summary>
    public void Disable()
    {
        Enabled = false;
    }
}
