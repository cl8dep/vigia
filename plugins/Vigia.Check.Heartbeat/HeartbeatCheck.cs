using Vigia.Plugins;

namespace Vigia.Check.Heartbeat;

/// <summary>
/// Push-based check for cron jobs and anything Vigia cannot reach: the job calls the <c>ping</c> webhook when it runs,
/// and the check goes down when no ping arrives within <c>every + grace</c>.
/// </summary>
/// <remarks>
/// Reads webhook receipts, which live on the control plane, so heartbeat checks run on the built-in worker.
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
        var receipts = ctx.GetRequiredService<IWebhookReceipts>();
        var last = await receipts.LastReceivedAsync(PingWebhook, ct);
        var allowed = config.Every + config.Grace;
        var now = ctx.Time.GetUtcNow();

        if (last is null)
        {
            // Never pinged: measure from when the check started listening, so a job that never runs still goes down.
            var waited = now - await receipts.ListeningSinceAsync(ct);
            var silence = new Measurement(SinceLastPing, waited.TotalSeconds);
            if (waited <= allowed)
            {
                return ProbeResult.Error($"Waiting for the first ping ({Minutes(waited)} of {Minutes(allowed)} min).");
            }

            return ProbeResult.Down($"No ping received since the check was created {Minutes(waited)} min ago; expected within {Minutes(allowed)} min.", silence);
        }

        var age = now - last.Value;
        var since = new Measurement(SinceLastPing, age.TotalSeconds);
        if (age > allowed)
        {
            return ProbeResult.Down($"Last ping {Minutes(age)} min ago; expected within {Minutes(allowed)} min.", since);
        }

        return ProbeResult.Up(since);
    }

    private static string Minutes(TimeSpan value)
    {
        return value.TotalMinutes.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }
}
