using Mediator;

namespace Vigia.Application.Services.CreateService;

/// <summary>
/// Creates a service.
/// </summary>
/// <param name="Slug">Stable identity, unique.</param>
/// <param name="Spec">Service definition.</param>
public sealed record CreateServiceCommand(string Slug, ServiceSpec Spec) : ICommand<ServiceDto>;
