using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;
using Vigia.Domain.Alerts;
using Vigia.Domain.Checks;
using Vigia.Domain.Results;
using Vigia.Domain.Rules;

namespace Vigia.Application.Rules;

/// <summary>
/// Evaluates the rules that target a check against a new result and opens, updates or resolves alerts.
/// Changes are tracked on the context; the caller saves them together with the result.
/// </summary>
/// <remarks>
/// Error results say nothing about the target, so they neither fire nor recover. Results missing a rule's
/// dimension are skipped for that rule. Quorum across workers comes with remote workers; today every result counts.
/// </remarks>
public sealed class RuleEvaluator(IAppDbContext db, IPluginRegistry registry)
{
    /// <summary>Evaluates <paramref name="result"/>, which is added to the context but not saved yet.</summary>
    public async Task EvaluateAsync(Check check, CheckResult result, CancellationToken ct)
    {
        if (result.Outcome == ResultOutcome.Error)
        {
            return;
        }

        var rules = (await db.Rules.Where(r => r.Enabled).ToListAsync(ct))
            .Where(r => r.Targets(check.Id, check.Tags))
            .ToList();
        if (rules.Count == 0)
        {
            return;
        }

        // Enough history for the deepest rule, with room for results a threshold rule skips.
        var depth = rules.Max(r => Math.Max(r.For, r.RecoverAfter)) * 4;
        var previous = await db.CheckResults.AsNoTracking()
            .Where(r => r.CheckId == check.Id && r.Outcome != ResultOutcome.Error && r.ObservedAt <= result.ObservedAt && r.Id != result.Id)
            .OrderByDescending(r => r.ObservedAt)
            .Take(depth)
            .ToListAsync(ct);
        List<CheckResult> recent = [result, .. previous];

        var ruleIds = rules.Select(r => r.Id).ToList();
        var firing = await db.Alerts
            .Where(a => a.CheckId == check.Id && a.State == AlertState.Firing && ruleIds.Contains(a.RuleId))
            .ToDictionaryAsync(a => a.RuleId, ct);

        registry.TryGetCheck(check.Plugin, out var plugin);
        foreach (var rule in rules)
        {
            Evaluate(rule, check, recent, firing.GetValueOrDefault(rule.Id), plugin);
        }
    }

    private void Evaluate(Rule rule, Check check, IReadOnlyList<CheckResult> recent, Alert? alert, CheckPlugin? plugin)
    {
        var evaluable = recent.Where(r => rule.Condition == ConditionKind.Down || r.Measurements.ContainsKey(rule.Dimension!)).ToList();

        // The new result must be evaluable for this rule, or it changes nothing.
        if (evaluable.Count == 0 || evaluable[0] != recent[0])
        {
            return;
        }

        var latest = evaluable[0];
        var matching = evaluable.TakeWhile(r => Matches(rule, r)).Count();
        var passing = evaluable.TakeWhile(r => !Matches(rule, r)).Count();

        if (alert is null && matching >= rule.For)
        {
            db.Alerts.Add(new Alert(rule.Id, check.Id, rule.Severity, Describe(rule, latest, plugin), latest.ObservedAt));
        }
        else if (alert is not null && matching > 0)
        {
            alert.Seen(Describe(rule, latest, plugin), latest.ObservedAt);
        }
        else if (alert is not null && passing >= rule.RecoverAfter)
        {
            alert.Resolve(latest.ObservedAt);
        }
    }

    private static bool Matches(Rule rule, CheckResult result)
    {
        return rule.Condition switch
        {
            ConditionKind.Down => result.Outcome == ResultOutcome.Down,
            ConditionKind.Above => result.Measurements[rule.Dimension!] > rule.Threshold,
            _ => result.Measurements[rule.Dimension!] < rule.Threshold,
        };
    }

    private static string Describe(Rule rule, CheckResult result, CheckPlugin? plugin)
    {
        if (rule.Condition == ConditionKind.Down)
        {
            return result.Message ?? "Check is down.";
        }

        var unit = plugin?.Dimensions.FirstOrDefault(d => d.Name == rule.Dimension)?.Unit ?? string.Empty;
        var comparison = rule.Condition == ConditionKind.Above ? "above" : "below";
        return $"{rule.Dimension} is {result.Measurements[rule.Dimension!]:0.##}{unit}, {comparison} {rule.Threshold:0.##}{unit}.";
    }
}
