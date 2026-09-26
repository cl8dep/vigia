using System.Net;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// <c>vigia.check.dns</c> against local DNS servers.
/// </summary>
public sealed class DnsCheckTests
{
    private const string Id = "vigia.check.dns";
    private const string Host = "api.vigia.test";

    [Fact]
    public async Task Up_when_the_answer_contains_the_expected_values()
    {
        await using var server = Server("10.0.0.1", "10.0.0.2");

        var result = await PluginHarness.ProbeAsync(Id, new { host = Host, resolvers = new[] { server.Endpoint }, expected = new[] { "10.0.0.2" } });

        Assert.Equal(Outcome.Up, result.Outcome);
        Assert.Equal(0, result.Measure("failed-resolvers"));
    }

    [Fact]
    public async Task Down_when_an_expected_value_is_missing()
    {
        await using var server = Server("10.0.0.9");

        var result = await PluginHarness.ProbeAsync(Id, new { host = Host, resolvers = new[] { server.Endpoint }, expected = new[] { "10.0.0.1" } });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Contains("missing 10.0.0.1", result.Message);
    }

    [Fact]
    public async Task Down_on_nxdomain()
    {
        await using var server = Server("10.0.0.1");

        var result = await PluginHarness.ProbeAsync(Id, new { host = "missing.vigia.test", resolvers = new[] { server.Endpoint } });

        Assert.Equal(Outcome.Down, result.Outcome);
    }

    [Fact]
    public async Task Pinpoints_the_resolver_that_disagrees()
    {
        await using var good = Server("10.0.0.1");
        await using var bad = Server("10.6.6.6");

        var result = await PluginHarness.ProbeAsync(Id, new { host = Host, resolvers = new[] { good.Endpoint, bad.Endpoint }, expected = new[] { "10.0.0.1" } });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Equal(1, result.Measure("failed-resolvers"));
        Assert.Contains(bad.Endpoint, result.Message);
        Assert.DoesNotContain(good.Endpoint, result.Message);
    }

    [Fact]
    public async Task Down_when_the_resolver_does_not_answer()
    {
        var result = await PluginHarness.ProbeAsync(Id, new { host = Host, resolvers = new[] { $"127.0.0.1:{FreePort.Udp()}" }, timeout = "1s" });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Equal(1, result.Measure("failed-resolvers"));
    }

    [Fact]
    public async Task Invalid_resolver_is_a_config_error()
    {
        var result = await PluginHarness.ProbeAsync(Id, new { host = Host, resolvers = new[] { "not-an-ip" } });

        Assert.Equal(Outcome.Error, result.Outcome);
    }

    private static LocalDnsServer Server(params string[] addresses)
    {
        return new LocalDnsServer(new Dictionary<string, IPAddress[]> { [Host] = [.. addresses.Select(IPAddress.Parse)] });
    }
}
