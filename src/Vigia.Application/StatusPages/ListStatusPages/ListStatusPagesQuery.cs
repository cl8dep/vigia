using Mediator;

namespace Vigia.Application.StatusPages.ListStatusPages;

/// <summary>
/// Lists all status pages ordered by slug.
/// </summary>
public sealed record ListStatusPagesQuery : IQuery<IReadOnlyList<StatusPageDto>>;
