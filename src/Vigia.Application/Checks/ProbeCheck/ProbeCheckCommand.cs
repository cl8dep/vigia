using Mediator;

namespace Vigia.Application.Checks.ProbeCheck;

/// <summary>
/// Runs one probe of a check right now, in-process, and returns the result without storing it.
/// </summary>
/// <param name="Slug">Check slug.</param>
public sealed record ProbeCheckCommand(string Slug) : ICommand<ProbeResultDto>;
