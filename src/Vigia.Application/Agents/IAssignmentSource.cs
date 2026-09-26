namespace Vigia.Application.Agents;

/// <summary>
/// Where an agent gets its checks: the database for the built-in agent, the control plane for remote agents.
/// </summary>
public interface IAssignmentSource
{
    /// <summary>Returns the full current set of assignments.</summary>
    Task<IReadOnlyList<CheckAssignment>> GetAsync(CancellationToken ct);
}
