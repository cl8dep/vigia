using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vigia.Infrastructure.Results;

/// <summary>
/// Runs <see cref="ResultMaintenance"/> on startup and then every <see cref="RetentionOptions.RunInterval"/>.
/// </summary>
/// <remarks>
/// Single-replica for now. With several control plane replicas this needs a lock (Postgres advisory lock or Quartz clustering).
/// </remarks>
public sealed class ResultMaintenanceService(
    IServiceScopeFactory scopes,
    IOptions<RetentionOptions> options,
    TimeProvider time,
    ILogger<ResultMaintenanceService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(options.Value.RunInterval, time);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var report = await scope.ServiceProvider.GetRequiredService<ResultMaintenance>().RunOnceAsync(time.GetUtcNow(), stoppingToken);
                logger.LogInformation(
                    "Result maintenance: {Written} rollup(s) written from {From}, {Results} result(s) and {Rollups} rollup(s) deleted",
                    report.RollupsWritten, report.RolledUpFrom, report.ResultsDeleted, report.RollupsDeleted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Result maintenance failed; will retry on the next run");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
