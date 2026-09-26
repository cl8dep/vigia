using Mediator;

namespace Vigia.Application.Workers.UpdateWorker;

/// <summary>
/// Replaces a worker's name, region and tags. Credentials are untouched.
/// </summary>
/// <param name="Slug">Worker to update.</param>
/// <param name="Spec">New definition.</param>
public sealed record UpdateWorkerCommand(string Slug, WorkerSpec Spec) : ICommand<WorkerDto>;
