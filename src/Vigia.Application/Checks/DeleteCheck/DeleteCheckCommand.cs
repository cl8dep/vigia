using Mediator;

namespace Vigia.Application.Checks.DeleteCheck;

/// <summary>
/// Deletes a check together with its results and rollups.
/// </summary>
/// <param name="Slug">Check to delete.</param>
public sealed record DeleteCheckCommand(string Slug) : ICommand;
