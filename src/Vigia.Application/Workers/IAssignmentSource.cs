namespace Vigia.Application.Workers;

/// <summary>
/// Where a worker gets its checks: the database for the built-in worker, the control plane for remote workers.
/// </summary>
public interface IAssignmentSource
{
    /// <summary>Returns the full current set of assignments.</summary>
    Task<IReadOnlyList<CheckAssignment>> GetAsync(CancellationToken ct);
}
