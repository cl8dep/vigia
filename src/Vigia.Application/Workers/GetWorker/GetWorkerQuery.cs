using Mediator;

namespace Vigia.Application.Workers.GetWorker;

/// <summary>
/// Gets a worker by slug.
/// </summary>
/// <param name="Slug">Worker slug.</param>
public sealed record GetWorkerQuery(string Slug) : IQuery<WorkerDto>;
