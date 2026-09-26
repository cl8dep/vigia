namespace Vigia.Application.Workers;

/// <summary>
/// Where a worker sends finished probes: the database for the built-in worker, the control plane for remote workers.
/// </summary>
public interface IResultSink
{
    /// <summary>Stores or forwards one probe result.</summary>
    Task WriteAsync(ProbeRecord record, CancellationToken ct);
}
