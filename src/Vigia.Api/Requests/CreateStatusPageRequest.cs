using Vigia.Application.StatusPages;

namespace Vigia.Api.Requests;

/// <summary>
/// Body of <c>POST /api/v1/status-pages</c>: a page definition plus its slug.
/// </summary>
public sealed record CreateStatusPageRequest : StatusPageSpec
{
    /// <summary>Stable identity; also the public URL segment.</summary>
    public required string Slug { get; init; }
}
