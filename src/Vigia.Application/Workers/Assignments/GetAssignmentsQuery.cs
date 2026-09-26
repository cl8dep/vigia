using Mediator;

namespace Vigia.Application.Workers.Assignments;

/// <summary>
/// Assignments of an authenticated remote worker.
/// </summary>
/// <param name="WorkerId">Worker, from its credential.</param>
public sealed record GetAssignmentsQuery(Guid WorkerId) : IQuery<AssignmentsDto>;
