using System.Diagnostics.Metrics;

namespace Vigia.Application.Workers;

/// <summary>
/// Worker runtime metrics (meter <c>Vigia.Worker</c>). Tell whether a worker keeps up with its checks.
/// </summary>
public sealed class WorkerMetrics
{
    /// <summary>Meter name, for exporters and listeners.</summary>
    public const string MeterName = "Vigia.Worker";

    private readonly Counter<long> _started;
    private readonly Counter<long> _skipped;
    private readonly Histogram<double> _slotWait;
    private readonly Histogram<double> _duration;

    /// <summary>Creates the instruments.</summary>
    public WorkerMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(MeterName);
        _started = meter.CreateCounter<long>("vigia.worker.probes.started", description: "Probes started.");
        _skipped = meter.CreateCounter<long>(
            "vigia.worker.probes.skipped",
            description: "Ticks skipped because the previous probe of the same check was still running or waiting. Sustained growth means the worker is overloaded.");
        _slotWait = meter.CreateHistogram<double>(
            "vigia.worker.probes.slot_wait", unit: "ms", description: "Time a due probe waited for a free concurrency slot.");
        _duration = meter.CreateHistogram<double>("vigia.worker.probes.duration", unit: "ms", description: "Probe duration.");
    }

    /// <summary>A probe started.</summary>
    public void Started(string plugin)
    {
        _started.Add(1, new KeyValuePair<string, object?>("plugin", plugin));
    }

    /// <summary>A tick was skipped.</summary>
    public void Skipped(string plugin)
    {
        _skipped.Add(1, new KeyValuePair<string, object?>("plugin", plugin));
    }

    /// <summary>A due probe waited this long for a slot.</summary>
    public void SlotWait(TimeSpan wait)
    {
        _slotWait.Record(wait.TotalMilliseconds);
    }

    /// <summary>A probe finished with this outcome and duration.</summary>
    public void Finished(string plugin, string outcome, double durationMs)
    {
        _duration.Record(durationMs, new KeyValuePair<string, object?>("plugin", plugin), new KeyValuePair<string, object?>("outcome", outcome));
    }
}
