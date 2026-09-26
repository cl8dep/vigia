using Mediator;

namespace Vigia.Application.Workers.Heartbeat;

/// <summary>
/// An authenticated worker reports that it is alive.
/// </summary>
/// <param name="WorkerId">Worker, from its credential.</param>
/// <param name="Version">Worker software version.</param>
public sealed record HeartbeatCommand(Guid WorkerId, string? Version) : ICommand<WorkerDto>;
