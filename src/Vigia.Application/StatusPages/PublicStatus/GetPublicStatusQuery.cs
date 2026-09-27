using Mediator;

namespace Vigia.Application.StatusPages.PublicStatus;

/// <summary>
/// The public view of a status page.
/// </summary>
/// <param name="Slug">Page slug.</param>
/// <param name="Days">Days of history per component, 1 to 90.</param>
public sealed record GetPublicStatusQuery(string Slug, int Days = 90) : IQuery<PublicStatusDto>;
