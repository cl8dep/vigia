using Mediator;

namespace Vigia.Application.Services.GetService;

/// <summary>
/// Gets a service by slug.
/// </summary>
/// <param name="Slug">Service slug.</param>
public sealed record GetServiceQuery(string Slug) : IQuery<ServiceDto>;
