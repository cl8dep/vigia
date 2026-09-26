using System.Text.Json;
using Microsoft.Extensions.Options;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Plugins;
using Vigia.Application.Webhooks;
using Vigia.Plugins;

namespace Vigia.Application.Agents;

/// <summary>
/// <see cref="IProbeExecutor"/> that runs loaded check plugins with a timeout and error isolation.
/// </summary>
/// <param name="registry">Loaded plugins.</param>
/// <param name="context">Host services shared by all probes.</param>
/// <param name="options">Agent settings.</param>
/// <param name="receipts">Webhook receipts; null on remote agents, where they do not exist.</param>
public sealed class ProbeExecutor(IPluginRegistry registry, ICheckContext context, IOptions<AgentOptions> options, IWebhookReceiptStore? receipts = null)
    : IProbeExecutor
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
            var probeContext = new ProbeContext(context, assignment.CheckId, plugin, receipts);
            return RejectUndeclaredDimensions(await plugin.Check.ProbeAsync(config, probeContext, cts.Token), plugin);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ProbeResult.Error($"Probe exceeded {timeout.TotalSeconds}s.");
        }
        catch (InvalidOperationException ex)
        {
            // Capability violations (undeclared service, receipts off the control plane) are the plugin's fault, not the target's.
            return ProbeResult.Error(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProbeResult.Error($"Plugin failed: {ex.Message}");
        }
    }

    private static ProbeResult RejectUndeclaredDimensions(ProbeResult result, CheckPlugin plugin)
    {
        var declared = plugin.Dimensions.Select(d => d.Name).ToHashSet(StringComparer.Ordinal);
        var undeclared = result.Measurements.Select(m => m.Dimension).Where(d => !declared.Contains(d)).Distinct().ToList();
        if (undeclared.Count == 0)
        {
            return result;
        }

        return ProbeResult.Error($"Plugin '{plugin.Id}' reported undeclared dimension(s) {string.Join(", ", undeclared)}. Declare them in check.dimensions.");
    }
}
