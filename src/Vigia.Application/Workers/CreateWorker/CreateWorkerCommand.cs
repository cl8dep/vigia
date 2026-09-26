using Mediator;

namespace Vigia.Application.Workers.CreateWorker;

/// <summary>
/// Registers a remote worker and issues its first enrollment token.
/// </summary>
/// <param name="Slug">Stable identity, unique.</param>
/// <param name="Spec">Worker definition.</param>
public sealed record CreateWorkerCommand(string Slug, WorkerSpec Spec) : ICommand<WorkerDto>;
