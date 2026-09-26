using Mediator;

namespace Vigia.Application.Workers.RevokeWorker;

/// <summary>
/// Invalidates a worker's credential immediately, for example when its host is compromised.
/// </summary>
/// <param name="Slug">Worker slug.</param>
public sealed record RevokeWorkerCommand(string Slug) : ICommand<WorkerDto>;
