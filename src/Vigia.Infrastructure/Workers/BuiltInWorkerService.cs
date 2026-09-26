using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vigia.Application.Workers;

namespace Vigia.Infrastructure.Workers;

/// <summary>
/// Hosts the built-in worker inside the control plane, so a single-node install monitors with nothing else.
/// </summary>
public sealed class BuiltInWorkerService(WorkerScheduler scheduler, IOptions<WorkerOptions> options, ILogger<BuiltInWorkerService> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.BuiltInEnabled)
        {
            logger.LogInformation("Built-in worker disabled");
            return;
        }

        logger.LogInformation("Built-in worker '{Name}' started", options.Value.Name);
        await scheduler.RunAsync(stoppingToken);
    }
}
