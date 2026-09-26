using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Plugins;
using Vigia.Infrastructure.Checks;
using Vigia.Infrastructure.Plugins;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// Calls the loaded HTTP plugin through the host context against a real local server.
/// </summary>
public sealed class HttpCheckProbeTests : IAsyncLifetime
{
    private readonly CheckPlugin _plugin;
    private readonly ICheckContext _context;
    private LocalHttpServer _server = null!;

    /// <summary>Loads the plugin and builds the host context the same way the app does.</summary>
    public HttpCheckProbeTests()
    {
        new PluginLoader(NullLogger<PluginLoader>.Instance).Load(RepoPaths.Plugins).TryGetCheck("vigia.check.http", out _plugin);

        var services = new ServiceCollection();
        services.AddHttpClient(HostCheckContext.HttpClientName);
        var provider = services.BuildServiceProvider();
        _context = new HostCheckContext(provider.GetRequiredService<IHttpClientFactory>(), TimeProvider.System);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _server = await LocalHttpServer.StartAsync(TestContext.Current.CancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task Up_when_status_and_body_match()
    {
        var result = await ProbeAsync(new { url = $"{_server.BaseUrl}/ok", bodyContains = "healthy" });

        Assert.Equal(Outcome.Up, result.Outcome);
        Assert.Contains(result.Measurements, m => m.Dimension == "latency" && m.Value >= 0);
        Assert.Contains(result.Measurements, m => m.Dimension == "status-code" && m.Value == 200);
    }

    [Fact]
    public async Task Down_when_status_is_unexpected()
    {
        var result = await ProbeAsync(new { url = $"{_server.BaseUrl}/fail" });

        Assert.Equal(Outcome.Down, result.Outcome);
        Assert.Contains("Status 500", result.Message);
    }

    [Fact]
    public async Task Down_when_nothing_listens()
    {
        var result = await ProbeAsync(new { url = "http://127.0.0.1:1/", timeout = "2s" });

        Assert.Equal(Outcome.Down, result.Outcome);
    }

    [Fact]
    public void Config_errors_point_at_the_field()
    {
        var missingUrl = Assert.Throws<ValidationException>(() => Bind(new { method = "GET" }));
        Assert.Contains("config.url", missingUrl.Errors.Keys);

        var unknownField = Assert.Throws<ValidationException>(() => Bind(new { url = "https://example.com", urll = "typo" }));
        Assert.Contains(unknownField.Errors.Keys, k => k.StartsWith("config", StringComparison.Ordinal));

        var badDuration = Assert.Throws<ValidationException>(() => Bind(new { url = "https://example.com", timeout = "soon" }));
        Assert.Contains("config.timeout", badDuration.Errors.Keys);
    }

    [Fact]
    public void Normalized_config_fills_defaults()
    {
        var bound = Bind(new { url = "https://example.com" });

        Assert.Equal("GET", bound.Normalized.GetProperty("method").GetString());
        Assert.Equal("10s", bound.Normalized.GetProperty("timeout").GetString());
    }

    private BoundConfig Bind(object config)
    {
        return ConfigBinder.Bind(_plugin, JsonSerializer.SerializeToElement(config), "config");
    }

    private Task<ProbeResult> ProbeAsync(object config)
    {
        return _plugin.Check.ProbeAsync(Bind(config).Value, _context, TestContext.Current.CancellationToken);
    }
}
