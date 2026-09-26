using Vigia.Application.Workers;

namespace Vigia.Api.Requests;

/// <summary>
/// Body of <c>POST /api/v1/workers</c>: a worker definition plus its slug.
/// </summary>
public sealed record CreateWorkerRequest : WorkerSpec
{
    /// <summary>Stable identity, unique.</summary>
    public required string Slug { get; init; }
}
