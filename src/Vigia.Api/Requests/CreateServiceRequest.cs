using Vigia.Application.Services;

namespace Vigia.Api.Requests;

/// <summary>
/// Body of <c>POST /api/v1/services</c>: a service definition plus its slug.
/// </summary>
public sealed record CreateServiceRequest : ServiceSpec
{
    /// <summary>Stable identity, unique.</summary>
    public required string Slug { get; init; }
}
