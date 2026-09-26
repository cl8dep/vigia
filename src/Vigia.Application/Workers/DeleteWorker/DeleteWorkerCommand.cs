using Mediator;

namespace Vigia.Application.Workers.DeleteWorker;

/// <summary>
/// Deletes a remote worker. Its credential stops working immediately; its past results are kept.
/// </summary>
/// <param name="Slug">Worker to delete.</param>
public sealed record DeleteWorkerCommand(string Slug) : ICommand;
