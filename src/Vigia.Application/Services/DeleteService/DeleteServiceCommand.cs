using Mediator;

namespace Vigia.Application.Services.DeleteService;

/// <summary>
/// Deletes a service. Edges from services that depended on it go with it; its checks are untouched.
/// </summary>
/// <param name="Slug">Service to delete.</param>
public sealed record DeleteServiceCommand(string Slug) : ICommand;
