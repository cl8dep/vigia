using Mediator;

namespace Vigia.Application.Services.UpdateService;

/// <summary>
/// Replaces a service's definition, dependencies included.
/// </summary>
/// <param name="Slug">Service to update.</param>
/// <param name="Spec">New definition.</param>
public sealed record UpdateServiceCommand(string Slug, ServiceSpec Spec) : ICommand<ServiceDto>;
