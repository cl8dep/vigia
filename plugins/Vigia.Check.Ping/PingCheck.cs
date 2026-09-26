using System.Net.NetworkInformation;
using Vigia.Plugins;

namespace Vigia.Check.Ping;

/// <summary>
/// Sends ICMP echo requests. Down only when every request is lost; partial loss is reported as a dimension.
/// </summary>
/// <remarks>
/// On Linux, ICMP needs the worker to run with <c>CAP_NET_RAW</c> or unprivileged ICMP sockets enabled
/// (<c>net.ipv4.ping_group_range</c>); otherwise .NET falls back to the <c>ping</c> binary if present.
/// </remarks>
public sealed class PingCheck : Check<PingCheckConfig>
{
    private const string PacketLoss = "packet-loss";

    /// <inheritdoc />
    public override async Task<ProbeResult> ProbeAsync(PingCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        using var ping = new System.Net.NetworkInformation.Ping();
        var replies = new List<long>();
        string? lastFailure = null;

        for (var i = 0; i < config.Count; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync(config.Host, config.Timeout, cancellationToken: ct);
                if (reply.Status == IPStatus.Success)
                {
                    replies.Add(reply.RoundtripTime);
                }
                else
                {
                    lastFailure = reply.Status.ToString();
                }
            }
            catch (PingException ex)
            {
                // Name resolution or permission problems fail every request the same way.
                return ProbeResult.Error($"Ping failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        var loss = new Measurement(PacketLoss, 100.0 * (config.Count - replies.Count) / config.Count);
        if (replies.Count == 0)
        {
            return ProbeResult.Down($"No replies to {config.Count} echo request(s): {lastFailure}.", loss);
        }

        return ProbeResult.Up(Measurement.Latency(replies.Average()), loss);
    }
}
