using Mediator;

namespace Vigia.Application.Alerts.ListAlerts;

/// <summary>
/// Alerts, newest first, optionally filtered by state and check.
/// </summary>
/// <param name="State"><c>firing</c>, <c>resolved</c>, or null for both.</param>
/// <param name="Check">Check slug, or null for all.</param>
/// <param name="Limit">1 to 500.</param>
public sealed record ListAlertsQuery(string? State, string? Check, int Limit) : IQuery<IReadOnlyList<AlertDto>>;
