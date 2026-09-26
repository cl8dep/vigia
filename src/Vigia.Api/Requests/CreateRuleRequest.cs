using Vigia.Application.Rules;

namespace Vigia.Api.Requests;

/// <summary>
/// Body of <c>POST /api/v1/rules</c>: a rule definition plus its slug.
/// </summary>
public sealed record CreateRuleRequest : RuleSpec
{
    /// <summary>Stable identity, unique.</summary>
    public required string Slug { get; init; }
}
