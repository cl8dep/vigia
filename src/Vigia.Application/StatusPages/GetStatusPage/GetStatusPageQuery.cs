using Mediator;

namespace Vigia.Application.StatusPages.GetStatusPage;

/// <summary>
/// Gets a status page's configuration by slug.
/// </summary>
/// <param name="Slug">Page slug.</param>
public sealed record GetStatusPageQuery(string Slug) : IQuery<StatusPageDto>;
