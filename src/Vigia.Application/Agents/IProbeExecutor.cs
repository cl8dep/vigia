namespace Vigia.Application.Agents;

/// <summary>
/// Runs one probe of an assignment. Never throws for plugin failures; they become error results.
/// </summary>
public interface IProbeExecutor
{
    /// <summary>Probes the check once.</summary>
    Task<ProbeRecord> ExecuteAsync(CheckAssignment assignment, CancellationToken ct);
}
