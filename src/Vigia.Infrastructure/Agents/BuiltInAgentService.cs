using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vigia.Application.Agents;

namespace Vigia.Infrastructure.Agents;

/// <summary>
/// Hosts the built-in agent inside the control plane, so a single-node install monitors with nothing else.
/// </summary>
public sealed class BuiltInAgentService(AgentScheduler scheduler, IOptions<AgentOptions> options, ILogger<BuiltInAgentService> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.BuiltInEnabled)
        {
            logger.LogInformation("Built-in agent disabled");
            return;
        }

        logger.LogInformation("Built-in agent '{Name}' started", options.Value.Name);
        await scheduler.RunAsync(stoppingToken);
    }
}
