namespace Vigia.Application.Workers.Enroll;

/// <summary>
/// Result of enrollment. The credential is shown once; the worker must store it.
/// </summary>
/// <param name="Worker">Worker slug.</param>
/// <param name="Credential">Send as <c>Authorization: Worker &lt;credential&gt;</c> on every worker request.</param>
public sealed record EnrollmentDto(string Worker, string Credential);
