using Vigia.Domain.Health;

namespace Vigia.Application.Health;

/// <summary>
/// Daily availability computed from a service's health transitions.
/// </summary>
/// <remarks>
/// Down counts as fully unavailable and partial outage as half; degraded and maintenance count as available.
/// Unknown time is excluded from the ratio.
/// </remarks>
public static class AvailabilityHistory
{
    /// <summary>The last <paramref name="days"/> days up to and including today, oldest first.</summary>
    /// <param name="changes">Transitions of one service, any order.</param>
    /// <param name="days">Number of days.</param>
    /// <param name="now">Current time.</param>
    public static IReadOnlyList<DayAvailability> Daily(IReadOnlyList<ServiceHealthChange> changes, int days, DateTimeOffset now)
    {
        var timeline = changes.OrderBy(c => c.At).ThenBy(c => c.Id).Select(c => (c.At, c.To)).ToList();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var result = new List<DayAvailability>(days);

        for (var i = days - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1) < now ? start.AddDays(1) : now;
            result.Add(Day(date, start, end, timeline));
        }

        return result;
    }

    private static DayAvailability Day(DateOnly date, DateTimeOffset start, DateTimeOffset end, IReadOnlyList<(DateTimeOffset At, HealthState To)> timeline)
    {
        var known = TimeSpan.Zero;
        var unavailable = TimeSpan.Zero;
        HealthState? worst = null;

        for (var i = 0; i < timeline.Count; i++)
        {
            var from = timeline[i].At > start ? timeline[i].At : start;
            var until = i + 1 < timeline.Count && timeline[i + 1].At < end ? timeline[i + 1].At : end;
            if (until <= from || timeline[i].At >= end)
            {
                continue;
            }

            var state = timeline[i].To;
            if (state == HealthState.Unknown)
            {
                continue;
            }

            var span = until - from;
            known += span;
            unavailable += state switch
            {
                HealthState.Down => span,
                HealthState.PartialOutage => span / 2,
                _ => TimeSpan.Zero,
            };
            worst = worst is null || state > worst ? state : worst;
        }

        return known == TimeSpan.Zero
            ? new DayAvailability(date, null, null)
            : new DayAvailability(date, worst, 1 - (unavailable / known));
    }
}
