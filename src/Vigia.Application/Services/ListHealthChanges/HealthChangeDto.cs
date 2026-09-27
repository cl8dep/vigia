namespace Vigia.Application.Services.ListHealthChanges;

/// <summary>
/// One transition in a service's health.
/// </summary>
/// <param name="From">Previous state, or null for the first one recorded.</param>
/// <param name="To">New state.</param>
/// <param name="Reason">Why, at the time.</param>
/// <param name="At">When.</param>
public sealed record HealthChangeDto(string? From, string To, string Reason, DateTimeOffset At);
