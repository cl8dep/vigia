using System.Text.Json;
using Microsoft.Extensions.Options;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Plugins;
using Vigia.Plugins;

namespace Vigia.Application.Agents;

/// <summary>
/// <see cref="IProbeExecutor"/> that runs loaded check plugins with a timeout and error isolation.
/// </summary>
public sealed class ProbeExecutor(IPluginRegistry registry, ICheckContext context, IOptions<AgentOptions> options) : IProbeExecutor
{
    /// <inheritdoc />
    public async Task<ProbeRecord> ExecuteAsync(CheckAssignment assignment, CancellationToken ct)
    {
        var observedAt = context.Time.GetUtcNow();
        var started = context.Time.GetTimestamp();
        var result = await RunAsync(assignment, ct);

        return new ProbeRecord(
            Guid.CreateVersion7(observedAt),
            assignment.CheckId,
            options.Value.Name,
            result.Outcome,
            result.Measurements.ToDictionary(m => m.Dimension, m => m.Value),
            result.Message,
            context.Time.GetElapsedTime(started).TotalMilliseconds,
            observedAt);
    }

    private async Task<ProbeResult> RunAsync(CheckAssignment assignment, CancellationToken ct)
    {
        if (!registry.TryGetCheck(assignment.Plugin, out var plugin))
        {
            return ProbeResult.Error($"Plugin '{assignment.Plugin}' is not installed.");
        }

        object config;
        try
        {
            config = ConfigBinder.Bind(plugin, JsonDocument.Parse(assignment.ConfigJson).RootElement, "config").Value;
        }
        catch (ValidationException ex)
        {
            return ProbeResult.Error($"Invalid config: {string.Join("; ", ex.Errors.SelectMany(e => e.Value))}");
        }

        var timeout = options.Value.ProbeTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            return await plugin.Check.ProbeAsync(config, context, cts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ProbeResult.Error($"Probe exceeded {timeout.TotalSeconds}s.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProbeResult.Error($"Plugin failed: {ex.Message}");
        }
    }
}
