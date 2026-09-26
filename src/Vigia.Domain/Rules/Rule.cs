using Vigia.Domain.Checks;
using Vigia.Domain.Common;
using Vigia.Domain.Tags;

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

    /// <inheritdoc />
    protected override TaggedEntity TagKind
    {
        get { return TaggedEntity.Rule; }
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

    /// <summary>Tags a targeted check must have, or <see cref="TagSelector.Any"/> for single-check rules.</summary>
    public TagSelector Selector { get; private set; } = TagSelector.Any;

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

    /// <summary>Workers that must agree for this rule, overriding the check's quorum; null uses the check's.</summary>
    public Quorum? Quorum { get; private set; }

    /// <summary>Severity of the alerts this rule creates.</summary>
    public Severity Severity { get; private set; }

    /// <summary>Disabled rules are not evaluated. Their firing alerts stay until resolved by hand.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Points the rule at one check.</summary>
    public void TargetCheck(Guid checkId)
    {
        CheckId = checkId;
        Selector = TagSelector.Any;
    }

    /// <summary>Points the rule at every check matching <paramref name="selector"/>.</summary>
    /// <exception cref="DomainException">The selector is empty.</exception>
    public void TargetSelector(TagSelector selector)
    {
        if (selector.IsEmpty)
        {
            throw new DomainException("A selector needs at least one tag.");
        }

        CheckId = null;
        Selector = selector;
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

    /// <summary>Overrides the check's quorum for this rule, for example quorum 1 as warning and majority as critical.</summary>
    public void OverrideQuorum(Quorum? quorum)
    {
        Quorum = quorum;
    }

    /// <summary>Whether this rule applies to a check with the given id and tags.</summary>
    public bool Targets(Guid checkId, IReadOnlyDictionary<string, string?> tags)
    {
        if (CheckId is not null)
        {
            return CheckId == checkId;
        }

        return !Selector.IsEmpty && Selector.Matches(tags);
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
