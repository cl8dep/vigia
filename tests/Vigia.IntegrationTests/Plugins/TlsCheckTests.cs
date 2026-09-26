using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// <c>vigia.check.tls</c> against a local TLS server with self-signed certificates.
/// </summary>
public sealed class TlsCheckTests
{
    private const string Id = "vigia.check.tls";
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Reports_days_to_expiry_when_chain_validation_is_off()
    {
        await using var server = new LocalTlsServer(Now.AddDays(-1), Now.AddDays(30));

        var result = await PluginHarness.ProbeAsync(Id, new { host = "localhost", port = server.Port, validateChain = false });

        Assert.Equal(Outcome.Up, result.Outcome);
        Assert.InRange(result.Measure("days-to-expiry"), 29.9, 30.1);
    }

    [Fact]
    public async Task Untrusted_certificate_is_down_with_chain_validation()
    {
        await using var server = new LocalTlsServer(Now.AddDays(-1), Now.AddDays(30));

        var result = await PluginHarness.ProbeAsync(Id, new { host = "localhost", port = server.Port });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Contains("RemoteCertificateChainErrors", result.Message);
        Assert.InRange(result.Measure("days-to-expiry"), 29.9, 30.1);
    }

    [Fact]
    public async Task Expired_certificate_is_down_even_without_validation()
    {
        await using var server = new LocalTlsServer(Now.AddDays(-30), Now.AddDays(-2));

        var result = await PluginHarness.ProbeAsync(Id, new { host = "localhost", port = server.Port, validateChain = false });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Contains("expired", result.Message);
        Assert.True(result.Measure("days-to-expiry") < 0);
    }

    [Fact]
    public async Task Down_when_nothing_listens()
    {
        var result = await PluginHarness.ProbeAsync(Id, new { host = "127.0.0.1", port = FreePort.Tcp() });

        Assert.Equal(Outcome.Down, result.Outcome);
    }
}
