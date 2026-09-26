using System.Diagnostics;
using System.Net.Sockets;
using Vigia.Plugins;

namespace Vigia.Check.Tcp;

/// <summary>
/// Opens a TCP connection. Up when the connection is accepted.
/// </summary>
public sealed class TcpCheck : Check<TcpCheckConfig>
{
    /// <inheritdoc />
    public override async Task<ProbeResult> ProbeAsync(TcpCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(config.Timeout);
        using var client = new TcpClient();

        var started = Stopwatch.GetTimestamp();
        try
        {
            await client.ConnectAsync(config.Host, config.Port, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ProbeResult.Down($"No connection within {config.Timeout.TotalSeconds}s.");
        }
        catch (SocketException ex)
        {
            return ProbeResult.Down($"Connection failed: {ex.SocketErrorCode}.");
        }

        return ProbeResult.Up(Measurement.Latency(Stopwatch.GetElapsedTime(started).TotalMilliseconds));
    }
}
