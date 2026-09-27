using Mediator;

namespace Vigia.Application.Services.ListServices;

/// <summary>
/// Lists all services ordered by slug.
/// </summary>
public sealed record ListServicesQuery : IQuery<IReadOnlyList<ServiceDto>>;
