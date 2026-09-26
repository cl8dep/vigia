using System.Collections.Concurrent;
using Vigia.Application.Agents;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Probe executor that counts calls per check and can hold every probe until released.
/// </summary>
public sealed class GatedProbeExecutor : IProbeExecutor
{
    private readonly ConcurrentDictionary<Guid, int> _started = new();
    private TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _running;
    private int _maxRunning;

    /// <summary>Creates an executor; open by default so probes finish immediately.</summary>
    public GatedProbeExecutor()
    {
        _gate.SetResult();
    }

    /// <summary>Probes currently running.</summary>
    public int Running
    {
        get { return Volatile.Read(ref _running); }
    }

    /// <summary>Highest number of probes seen running at once.</summary>
    public int MaxRunning
    {
        get { return Volatile.Read(ref _maxRunning); }
    }

    /// <summary>Total probes started across all checks.</summary>
    public int TotalStarted
    {
        get { return _started.Values.Sum(); }
    }

    /// <summary>Probes started for one check.</summary>
    public int Started(Guid checkId)
    {
        return _started.GetValueOrDefault(checkId);
    }

    /// <summary>From now on, probes wait until <see cref="Open"/> is called.</summary>
    public void Close()
    {
        _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Releases waiting probes and lets new ones finish immediately.</summary>
    public void Open()
    {
        _gate.TrySetResult();
    }

    /// <inheritdoc />
    public async Task<ProbeRecord> ExecuteAsync(CheckAssignment assignment, CancellationToken ct)
    {
        _started.AddOrUpdate(assignment.CheckId, 1, (_, n) => n + 1);
        var running = Interlocked.Increment(ref _running);
        InterlockedMax(ref _maxRunning, running);
        try
        {
            await _gate.Task.WaitAsync(ct);
            return new ProbeRecord(Guid.CreateVersion7(), assignment.CheckId, "test", Outcome.Up, new Dictionary<string, double>(), null, 0, DateTimeOffset.UtcNow);
        }
        finally
        {
            Interlocked.Decrement(ref _running);
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref target)))
        {
            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                break;
            }
        }
    }
}
