using Mediator;

namespace Vigia.Application.Workers.ListWorkers;

/// <summary>
/// Lists all workers ordered by slug.
/// </summary>
public sealed record ListWorkersQuery : IQuery<IReadOnlyList<WorkerDto>>;
