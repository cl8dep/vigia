using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vigia.Application.Health;
using Vigia.Application.Rules;

namespace Vigia.Infrastructure.Health;

/// <summary>
/// Recomputes every service's health on startup and then periodically: repairs any drift and applies inputs that
/// change with time rather than with events.
/// </summary>
public sealed class HealthReconciliationService(
    IServiceScopeFactory scopes,
    IOptions<AlertingOptions> options,
    TimeProvider time,
    ILogger<HealthReconciliationService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.HealthReconcileInterval, time);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<IServiceHealthUpdater>().RecomputeAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Service health reconciliation failed; will retry");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
