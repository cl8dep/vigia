using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vigia.Application.Agents;

/// <summary>
/// Runs assigned checks on their intervals. One loop over a priority queue ordered by next run time,
/// dispatching to a bounded number of concurrent probes.
/// </summary>
/// <remarks>
/// Missed ticks are skipped, never caught up. A probe still running when its next tick comes skips that tick,
/// so a slow target never piles up probes.
/// </remarks>
public sealed class AgentScheduler(
    IAssignmentSource source,
    IProbeExecutor executor,
    IResultSink sink,
    AgentMetrics metrics,
    IOptions<AgentOptions> options,
    TimeProvider time,
    ILogger<AgentScheduler> logger)
{
    private readonly Dictionary<Guid, ScheduledCheck> _checks = [];
    private readonly PriorityQueue<Guid, DateTimeOffset> _queue = new();
    private readonly List<Task> _inFlight = [];

    /// <summary>Runs until <paramref name="ct"/> is cancelled, then waits for in-flight probes to stop.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        var opts = options.Value;
        using var slots = new SemaphoreSlim(opts.MaxConcurrency);
        var nextRefresh = time.GetUtcNow();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var now = time.GetUtcNow();
                if (now >= nextRefresh)
                {
                    await RefreshAsync(now, ct);
                    nextRefresh = now + opts.RefreshInterval;
                }

                DispatchDue(now, slots, ct);

                var wakeAt = _queue.TryPeek(out _, out var due) && due < nextRefresh ? due : nextRefresh;
                var delay = wakeAt - time.GetUtcNow();
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, time, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }

        await Task.WhenAll(_inFlight);
    }

    private async Task RefreshAsync(DateTimeOffset now, CancellationToken ct)
    {
        IReadOnlyList<CheckAssignment> assignments;
        try
        {
            assignments = await source.GetAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Keep running the last known assignments; that is the point of the agent owning its schedule.
            logger.LogWarning(ex, "Could not refresh assignments; keeping {Count} current check(s)", _checks.Count);
            return;
        }

        var incoming = assignments.ToDictionary(a => a.CheckId);
        foreach (var removed in _checks.Keys.Where(id => !incoming.ContainsKey(id)).ToList())
        {
            _checks.Remove(removed);
        }

        foreach (var assignment in assignments)
        {
            if (!_checks.TryGetValue(assignment.CheckId, out var check))
            {
                check = new ScheduledCheck(assignment);
                _checks[assignment.CheckId] = check;
                Schedule(check, now + Jitter(assignment.Interval));
                continue;
            }

            if (check.Assignment.Version == assignment.Version)
            {
                continue;
            }

            var intervalChanged = check.Assignment.Interval != assignment.Interval;
            check.Assignment = assignment;
            if (intervalChanged)
            {
                Schedule(check, now + Jitter(assignment.Interval));
            }
        }
    }

    private void DispatchDue(DateTimeOffset now, SemaphoreSlim slots, CancellationToken ct)
    {
        while (_queue.TryPeek(out var id, out var due) && due <= now)
        {
            _queue.Dequeue();
            if (!_checks.TryGetValue(id, out var check) || check.NextRun != due)
            {
                continue;
            }

            var interval = check.Assignment.Interval;
            var next = due + interval;
            Schedule(check, next > now ? next : now + interval);

            if (!check.TryStart())
            {
                metrics.Skipped(check.Assignment.Plugin);
                logger.LogDebug("Check {Slug} is still running; skipping this tick", check.Assignment.Slug);
                continue;
            }

            _inFlight.Add(RunOneAsync(check, check.Assignment, slots, ct));
        }

        _inFlight.RemoveAll(t => t.IsCompleted);
    }

    private async Task RunOneAsync(ScheduledCheck check, CheckAssignment assignment, SemaphoreSlim slots, CancellationToken ct)
    {
        try
        {
            var waitStarted = time.GetTimestamp();
            await slots.WaitAsync(ct);
            metrics.SlotWait(time.GetElapsedTime(waitStarted));
            try
            {
                metrics.Started(assignment.Plugin);
                var record = await executor.ExecuteAsync(assignment, ct);
                metrics.Finished(assignment.Plugin, record.Outcome.ToString(), record.DurationMs);
                await sink.WriteAsync(record, ct);
            }
            finally
            {
                slots.Release();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Probe of check {Slug} failed outside the plugin", assignment.Slug);
        }
        finally
        {
            check.Finish();
        }
    }

    private void Schedule(ScheduledCheck check, DateTimeOffset at)
    {
        check.NextRun = at;
        _queue.Enqueue(check.Assignment.CheckId, at);
    }

    private TimeSpan Jitter(TimeSpan interval)
    {
        var max = interval < options.Value.InitialJitter ? interval : options.Value.InitialJitter;
        return max <= TimeSpan.Zero ? TimeSpan.Zero : TimeSpan.FromTicks(Random.Shared.NextInt64(max.Ticks));
    }
}
