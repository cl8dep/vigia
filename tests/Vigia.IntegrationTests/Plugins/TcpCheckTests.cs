using System.Net;
using System.Net.Sockets;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// <c>vigia.check.tcp</c> against real local sockets.
/// </summary>
public sealed class TcpCheckTests
{
    private const string Id = "vigia.check.tcp";

    [Fact]
    public async Task Up_when_the_port_accepts_connections()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var result = await PluginHarness.ProbeAsync(Id, new { host = "127.0.0.1", port = ((IPEndPoint)listener.LocalEndpoint).Port });

            Assert.Equal(Outcome.Up, result.Outcome);
            Assert.True(result.Measure("latency") >= 0);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task Down_when_the_connection_is_refused()
    {
        var result = await PluginHarness.ProbeAsync(Id, new { host = "127.0.0.1", port = FreePort.Tcp() });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Contains("ConnectionRefused", result.Message);
    }

    [Fact]
    public void Port_out_of_range_is_rejected()
    {
        var error = Assert.Throws<Application.Common.Exceptions.ValidationException>(() => PluginHarness.Bind(Id, new { host = "127.0.0.1", port = 70000 }));

        Assert.Contains("config.port", error.Errors.Keys);
    }
}
