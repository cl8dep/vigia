using Vigia.Domain.Alerts;

namespace Vigia.Application.Alerts;

/// <summary>
/// An alert with its rule and check slugs.
/// </summary>
/// <param name="Alert">Alert.</param>
/// <param name="Rule">Rule slug.</param>
/// <param name="Check">Check slug.</param>
public sealed record AlertRow(Alert Alert, string Rule, string Check);
