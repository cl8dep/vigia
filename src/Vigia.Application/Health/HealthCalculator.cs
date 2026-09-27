using System.Text.Json;
using Vigia.Domain.Checks;
using Vigia.Domain.Health;
using Vigia.Domain.Rules;
using Vigia.Domain.Services;

namespace Vigia.Application.Health;

/// <summary>
/// The single definition of service health (see <c>docs/services.md</c>). Pure: same inputs, same output.
/// </summary>
/// <remarks>
/// 1. A check is down with any firing critical alert, degraded with any firing warning, operational otherwise.
/// 2. A service takes the worst of its checks, or, with <see cref="Service.PartitionBy"/>, is down when every partition
///    is down and partially out when some are.
/// 3. Dependencies propagate: blocking passes the dependency's state through, soft caps it at degraded, advisory
///    has no effect. A dependency only ever makes things worse; a service with no checks of its own but with
///    blocking or soft dependencies of known state (a business service over technical ones) is operational when they are.
/// </remarks>
public static class HealthCalculator
{
    /// <summary>Partition name for checks that lack the partition tag.</summary>
    public const string Unpartitioned = "(none)";

    /// <summary>Computes the health of every service.</summary>
    /// <param name="services">All services, with dependencies.</param>
    /// <param name="checks">All checks.</param>
    /// <param name="firing">Severities of firing alerts, per check.</param>
    public static IReadOnlyDictionary<Guid, HealthResult> Compute(
        IReadOnlyList<Service> services, IReadOnlyList<Check> checks, IReadOnlyDictionary<Guid, IReadOnlyList<Severity>> firing)
    {
        var own = services.ToDictionary(s => s.Id, s => Own(s, checks, firing));
        var byId = services.ToDictionary(s => s.Id);
        var result = new Dictionary<Guid, HealthResult>();
        foreach (var service in services)
        {
            Resolve(service.Id, byId, own, result);
        }

        return result;
    }

    private static HealthResult Resolve(
        Guid id, IReadOnlyDictionary<Guid, Service> services, IReadOnlyDictionary<Guid, HealthResult> own, Dictionary<Guid, HealthResult> done)
    {
        if (done.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var service = services[id];
        var mine = own[id];
        var state = mine.State;
        var reasons = new List<string>();
        if (mine.State > HealthState.Operational)
        {
            reasons.Add(mine.Reason);
        }

        // The graph is acyclic (enforced on write), so this recursion terminates.
        var informed = false;
        foreach (var dependency in service.Dependencies)
        {
            if (!services.ContainsKey(dependency.DependsOnId))
            {
                continue;
            }

            var upstream = Resolve(dependency.DependsOnId, services, own, done);
            informed |= dependency.Mode != DependencyMode.Advisory && upstream.State != HealthState.Unknown;
            var effect = Propagate(upstream.State, dependency.Mode);
            if (effect > HealthState.Operational)
            {
                reasons.Add($"depends on {services[dependency.DependsOnId].Slug} ({Name(upstream.State)}, {Name(dependency.Mode)})");
                state = Worst(state, effect);
            }
        }

        if (state == HealthState.Unknown && informed)
        {
            state = HealthState.Operational;
        }

        var reason = reasons.Count > 0 ? string.Join("; ", reasons)
            : state == HealthState.Operational && mine.State == HealthState.Unknown ? "All dependencies operational."
            : mine.Reason;
        var result = new HealthResult(state, reason, mine.Partitions);
        done[id] = result;
        return result;
    }

    private static HealthResult Own(Service service, IReadOnlyList<Check> checks, IReadOnlyDictionary<Guid, IReadOnlyList<Severity>> firing)
    {
        var mine = service.Checks is null ? [] : checks.Where(c => service.Checks.Matches(c.Tags)).ToList();
        if (mine.Count == 0)
        {
            return new HealthResult(HealthState.Unknown, "No checks match this service.", new Dictionary<string, HealthState>());
        }

        var states = mine.Select(c => (Check: c, State: CheckState(firing.GetValueOrDefault(c.Id)))).ToList();
        if (service.PartitionBy is null)
        {
            var worst = states.Max(s => s.State);
            var bad = states.Where(s => s.State == worst && worst > HealthState.Operational).Select(s => s.Check.Slug).ToList();
            var reason = bad.Count == 0 ? "All checks operational." : $"{string.Join(", ", bad)} {Name(worst)}";
            return new HealthResult(worst, reason, new Dictionary<string, HealthState>());
        }

        var partitions = states
            .GroupBy(s => s.Check.Tags.GetValueOrDefault(service.PartitionBy) ?? Unpartitioned)
            .ToDictionary(g => g.Key, g => g.Max(s => s.State));
        var down = partitions.Where(p => p.Value == HealthState.Down).Select(p => p.Key).Order().ToList();

        if (down.Count == partitions.Count)
        {
            return new HealthResult(HealthState.Down, $"all {service.PartitionBy} partitions down", partitions);
        }

        if (down.Count > 0)
        {
            return new HealthResult(HealthState.PartialOutage, $"{string.Join(", ", down)} down", partitions);
        }

        var remaining = partitions.Values.Max();
        var degraded = partitions.Where(p => p.Value == HealthState.Degraded).Select(p => p.Key).Order().ToList();
        var text = remaining == HealthState.Degraded ? $"{string.Join(", ", degraded)} degraded" : "All partitions operational.";
        return new HealthResult(remaining, text, partitions);
    }

    private static HealthState CheckState(IReadOnlyList<Severity>? severities)
    {
        if (severities is null)
        {
            return HealthState.Operational;
        }

        if (severities.Contains(Severity.Critical))
        {
            return HealthState.Down;
        }

        return severities.Contains(Severity.Warning) ? HealthState.Degraded : HealthState.Operational;
    }

    private static HealthState Propagate(HealthState upstream, DependencyMode mode)
    {
        return mode switch
        {
            DependencyMode.Blocking => upstream is HealthState.Down or HealthState.PartialOutage or HealthState.Degraded ? upstream : HealthState.Operational,
            DependencyMode.Soft => upstream is HealthState.Down or HealthState.PartialOutage or HealthState.Degraded ? HealthState.Degraded : HealthState.Operational,
            _ => HealthState.Operational,
        };
    }

    private static HealthState Worst(HealthState a, HealthState b)
    {
        return a > b ? a : b;
    }

    private static string Name<T>(T value) where T : Enum
    {
        return JsonNamingPolicy.KebabCaseLower.ConvertName(value.ToString());
    }
}
