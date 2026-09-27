using Mediator;

namespace Vigia.Application.Services.ListHealthChanges;

/// <summary>
/// Health transitions of a service, newest first.
/// </summary>
/// <param name="Slug">Service slug.</param>
/// <param name="Limit">1 to 500.</param>
public sealed record ListHealthChangesQuery(string Slug, int Limit) : IQuery<IReadOnlyList<HealthChangeDto>>;
