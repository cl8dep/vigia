using Mediator;

namespace Vigia.Application.Workers.IssueEnrollmentToken;

/// <summary>
/// Issues a new one-time enrollment token for a worker, for example to reinstall it. Replaces any pending token;
/// the current credential keeps working until the worker enrolls again or is revoked.
/// </summary>
/// <param name="Slug">Worker slug.</param>
public sealed record IssueEnrollmentTokenCommand(string Slug) : ICommand<WorkerDto>;
