namespace Vigia.Application.Alerts;

/// <summary>
/// An alert as exposed by the API.
/// </summary>
/// <param name="Id">Alert id.</param>
/// <param name="Rule">Rule slug.</param>
/// <param name="Check">Check slug.</param>
/// <param name="Severity"><c>info</c>, <c>warning</c> or <c>critical</c>.</param>
/// <param name="State"><c>firing</c> or <c>resolved</c>.</param>
/// <param name="Message">Latest description of the problem.</param>
/// <param name="FiredAt">When it opened.</param>
/// <param name="LastSeenAt">Last matching result.</param>
/// <param name="ResolvedAt">When it resolved.</param>
/// <param name="Occurrences">Matching results while firing.</param>
public sealed record AlertDto(
    Guid Id,
    string Rule,
    string Check,
    string Severity,
    string State,
    string Message,
    DateTimeOffset FiredAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? ResolvedAt,
    long Occurrences);
