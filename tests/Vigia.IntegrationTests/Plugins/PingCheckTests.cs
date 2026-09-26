using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// <c>vigia.check.ping</c> against loopback.
/// </summary>
public sealed class PingCheckTests
{
    private const string Id = "vigia.check.ping";

    [Fact]
    public async Task Loopback_replies_without_loss()
    {
        var result = await PluginHarness.ProbeAsync(Id, new { host = "127.0.0.1", count = 2 });

        Assert.Equal(Outcome.Up, result.Outcome);
        Assert.Equal(0, result.Measure("packet-loss"));
    }

    [Fact]
    public async Task Unresolvable_host_is_an_error_not_down()
    {
        var result = await PluginHarness.ProbeAsync(Id, new { host = "does-not-exist.invalid", count = 1 });

        Assert.Equal(Outcome.Error, result.Outcome);
    }
}
