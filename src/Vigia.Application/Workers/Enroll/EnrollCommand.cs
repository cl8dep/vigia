using Mediator;

namespace Vigia.Application.Workers.Enroll;

/// <summary>
/// A worker exchanges its one-time enrollment token for a long-lived credential.
/// </summary>
/// <param name="Token">Enrollment token issued by an admin.</param>
/// <param name="Version">Worker software version.</param>
public sealed record EnrollCommand(string Token, string? Version) : ICommand<EnrollmentDto>;
