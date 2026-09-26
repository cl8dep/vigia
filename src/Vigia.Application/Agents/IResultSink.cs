namespace Vigia.Application.Agents;

/// <summary>
/// Where an agent sends finished probes: the database for the built-in agent, the control plane for remote agents.
/// </summary>
public interface IResultSink
{
    /// <summary>Stores or forwards one probe result.</summary>
    Task WriteAsync(ProbeRecord record, CancellationToken ct);
}
