using Mediator;

namespace Vigia.Application.Checks.GetCheck;

/// <summary>
/// Gets a check by slug.
/// </summary>
/// <param name="Slug">Check slug.</param>
public sealed record GetCheckQuery(string Slug) : IQuery<CheckDto>;
