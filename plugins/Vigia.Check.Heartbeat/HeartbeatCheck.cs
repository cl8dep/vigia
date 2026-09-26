using Vigia.Plugins;

namespace Vigia.Check.Heartbeat;

/// <summary>
/// Push-based check for cron jobs and anything Vigia cannot reach: the job calls the <c>ping</c> webhook when it runs,
/// and the check goes down when no ping arrives within <c>every + grace</c>.
/// </summary>
/// <remarks>
/// Reads webhook receipts, which live on the control plane, so heartbeat checks run on the built-in agent.
/// </remarks>
public sealed class HeartbeatCheck : Check<HeartbeatCheckConfig>
{
    /// <summary>Webhook the job calls, declared in plugin.json.</summary>
    public const string PingWebhook = "ping";

    /// <summary>Seconds since the last ping, declared in plugin.json.</summary>
    public const string SinceLastPing = "since-last-ping";

    /// <inheritdoc />
    public override async Task<ProbeResult> ProbeAsync(HeartbeatCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        var last = await ctx.GetRequiredService<IWebhookReceipts>().LastReceivedAsync(PingWebhook, ct);
        if (last is null)
        {
            // Not down: a new check has not had its first chance to ping yet.
            return ProbeResult.Error("No ping received yet.");
        }

        var age = ctx.Time.GetUtcNow() - last.Value;
        var since = new Measurement(SinceLastPing, age.TotalSeconds);
        var allowed = config.Every + config.Grace;
        if (age > allowed)
        {
            return ProbeResult.Down($"Last ping {age.TotalMinutes:0.#} min ago; expected within {allowed.TotalMinutes:0.#} min.", since);
        }

        return ProbeResult.Up(since);
    }
}
