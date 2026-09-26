using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vigia.Application.Workers;

namespace Vigia.Infrastructure.Workers;

/// <summary>
/// Hosts the built-in worker inside the control plane: registers it as a worker, reports it alive every minute,
/// and runs its scheduler. A single-node install monitors with nothing else.
/// </summary>
public sealed class BuiltInWorkerService(
    WorkerScheduler scheduler,
    IServiceScopeFactory scopes,
    IOptions<WorkerOptions> options,
    TimeProvider time,
    ILogger<BuiltInWorkerService> logger) : BackgroundService
{
    /// <summary>How often the built-in worker reports itself alive.</summary>
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(1);

    private static readonly string? Version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.BuiltInEnabled)
        {
            logger.LogInformation("Built-in worker disabled");
            return;
        }

        await ReportAsync(stoppingToken);
        logger.LogInformation("Built-in worker '{Name}' started in region {Region}", options.Value.Name, options.Value.Region);
        await Task.WhenAll(scheduler.RunAsync(stoppingToken), HeartbeatAsync(stoppingToken));
    }

    private async Task HeartbeatAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval, time);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                await ReportAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private async Task ReportAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<BuiltInWorkerRegistration>().EnsureAsync(Version, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Built-in worker could not report in");
        }
    }
}
