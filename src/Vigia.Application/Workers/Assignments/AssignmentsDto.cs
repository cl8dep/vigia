namespace Vigia.Application.Workers.Assignments;

/// <summary>
/// The full set of checks a worker must run. Always a complete snapshot, never a delta.
/// </summary>
/// <param name="Revision">Changes whenever the set or any check in it changes; lets workers skip unchanged snapshots.</param>
/// <param name="Checks">Assigned checks.</param>
public sealed record AssignmentsDto(string Revision, IReadOnlyList<AssignmentDto> Checks);
