namespace Vigia.Application.Health;

/// <summary>
/// Recomputes every service's health with <see cref="HealthCalculator"/>, stores the snapshots and appends transitions.
/// Joins the caller's transaction when there is one, so health changes commit together with their cause.
/// </summary>
public interface IServiceHealthUpdater
{
    /// <summary>Recomputes and stores health for all services.</summary>
    Task RecomputeAsync(CancellationToken ct);
}
