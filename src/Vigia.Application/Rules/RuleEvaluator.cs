using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Placement;
using Vigia.Application.Plugins;
using Vigia.Application.Webhooks;
using Vigia.Application.Workers;
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
/// Each worker keeps its own streak. The workers that count are the eligible ones that are online, plus any worker
/// with a result in the last two intervals; an online worker without results yet counts as not failing, and a worker
/// that stopped reporting is lost visibility, not an outage. A rule fires when at least the check's quorum of those
/// workers fail it, and resolves when fewer than the quorum have not yet recovered. Error results neither fire nor
/// recover; results missing a rule's dimension are skipped for that rule.
/// </remarks>
public sealed class RuleEvaluator(
    IAppDbContext db,
    IPluginRegistry registry,
    IOptions<AlertingOptions> alerting,
    IOptions<WorkerOptions> workerOptions,
    TimeProvider time)
{
    /// <summary>A worker counts while its latest result is at most this many check intervals older than the new one.</summary>
    public const int FreshIntervals = 2;

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

        // History for the deepest rule plus the freshness window, across all workers.
        var depth = rules.Max(r => Math.Max(r.For, r.RecoverAfter));
        var since = result.ObservedAt - (check.Interval * ((depth * 2) + FreshIntervals));
        var previous = await db.CheckResults.AsNoTracking()
            .Where(r => r.CheckId == check.Id && r.Outcome != ResultOutcome.Error && r.ObservedAt >= since && r.ObservedAt <= result.ObservedAt && r.Id != result.Id)
            .OrderByDescending(r => r.ObservedAt)
            .Take(1000)
            .ToListAsync(ct);

        var freshSince = result.ObservedAt - (check.Interval * FreshIntervals);
        var streams = new[] { result }.Concat(previous)
            .GroupBy(StreamOf)
            .Where(g => g.First().ObservedAt >= freshSince)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Online eligible workers count even before they report this check, so the first result cannot fire alone.
        registry.TryGetCheck(check.Plugin, out var plugin);
        var onlineSince = time.GetUtcNow() - WorkerDto.OnlineWindow;
        var online = (await db.Workers.AsNoTracking().Where(w => w.LastSeenAt >= onlineSince).ToListAsync(ct))
            .Where(w => CheckPlacement.CanRun(check, plugin, w))
            .Select(w => w.Slug)
            .Where(s => !streams.ContainsKey(s));
        foreach (var slug in online)
        {
            streams[slug] = [];
        }

        var ruleIds = rules.Select(r => r.Id).ToList();
        var firing = await db.Alerts
            .Where(a => a.CheckId == check.Id && a.State == AlertState.Firing && ruleIds.Contains(a.RuleId))
            .ToDictionaryAsync(a => a.RuleId, ct);

        var where = await WorkerLabelsAsync(streams.Keys, ct);
        var quorum = check.Quorum ?? Quorum.Parse(alerting.Value.DefaultQuorum);

        foreach (var rule in rules)
        {
            Evaluate(rule, check, result, streams, quorum, firing.GetValueOrDefault(rule.Id), plugin, where);
        }
    }

    private void Evaluate(
        Rule rule,
        Check check,
        CheckResult result,
        IReadOnlyDictionary<string, List<CheckResult>> streams,
        Quorum quorum,
        Alert? alert,
        CheckPlugin? plugin,
        IReadOnlyDictionary<string, string> where)
    {
        // The new result must be evaluable for this rule, or it changes nothing.
        if (!IsEvaluable(rule, result))
        {
            return;
        }

        // A worker with no evaluable results counts as healthy: it is online and has seen nothing wrong.
        var states = streams
            .Select(s => (Worker: s.Key, Results: s.Value.Where(r => IsEvaluable(rule, r)).ToList()))
            .Select(s => (
                s.Worker,
                Failing: s.Results.TakeWhile(r => Matches(rule, r)).Count(),
                Passing: s.Results.Count == 0 ? int.MaxValue : s.Results.TakeWhile(r => !Matches(rule, r)).Count()))
            .ToList();

        var required = quorum.Required(states.Count);
        var failing = states.Where(s => s.Failing >= rule.For).Select(s => s.Worker).ToList();
        var notRecovered = states.Count(s => s.Passing < rule.RecoverAfter);

        if (alert is null && failing.Count >= required)
        {
            db.Alerts.Add(new Alert(rule.Id, check.Id, rule.Severity, Describe(rule, result, plugin, failing, states.Count, where), result.ObservedAt));
        }
        else if (alert is not null && Matches(rule, result))
        {
            var failingNow = states.Where(s => s.Failing > 0).Select(s => s.Worker).ToList();
            alert.Seen(Describe(rule, result, plugin, failingNow, states.Count, where), result.ObservedAt);
        }
        else if (alert is not null && notRecovered < required)
        {
            alert.Resolve(result.ObservedAt);
        }
    }

    /// <summary>Webhook results belong to the built-in worker's stream: both come from the control plane.</summary>
    private string StreamOf(CheckResult result)
    {
        return result.Worker == WebhookContext.Worker ? workerOptions.Value.Name : result.Worker;
    }

    private async Task<IReadOnlyDictionary<string, string>> WorkerLabelsAsync(IEnumerable<string> slugs, CancellationToken ct)
    {
        var list = slugs.ToList();
        var regions = await db.Workers.AsNoTracking()
            .Where(w => list.Contains(w.Slug))
            .ToDictionaryAsync(w => w.Slug, w => w.Region, ct);
        return list.ToDictionary(s => s, s => regions.GetValueOrDefault(s) ?? s);
    }

    private static bool IsEvaluable(Rule rule, CheckResult result)
    {
        return result.Outcome != ResultOutcome.Error
            && (rule.Condition == ConditionKind.Down || result.Measurements.ContainsKey(rule.Dimension!));
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

    private static string Describe(
        Rule rule, CheckResult result, CheckPlugin? plugin, IReadOnlyList<string> failing, int reporting, IReadOnlyDictionary<string, string> where)
    {
        string text;
        if (rule.Condition == ConditionKind.Down)
        {
            text = result.Message ?? "Check is down.";
        }
        else
        {
            var unit = plugin?.Dimensions.FirstOrDefault(d => d.Name == rule.Dimension)?.Unit ?? string.Empty;
            var comparison = rule.Condition == ConditionKind.Above ? "above" : "below";
            text = $"{rule.Dimension} is {result.Measurements[rule.Dimension!]:0.##}{unit}, {comparison} {rule.Threshold:0.##}{unit}.";
        }

        // With one worker the location adds nothing.
        if (reporting <= 1)
        {
            return text;
        }

        var locations = string.Join(", ", failing.Select(w => where.GetValueOrDefault(w, w)).Order());
        return $"{text} (failing from {locations}: {failing.Count} of {reporting})";
    }
}
