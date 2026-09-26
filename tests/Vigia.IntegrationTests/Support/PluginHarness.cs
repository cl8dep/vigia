using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Vigia.Application.Plugins;
using Vigia.Infrastructure.Checks;
using Vigia.Infrastructure.Plugins;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Loads the real staged plugins and probes them through the host context, the same way workers do.
/// </summary>
public static class PluginHarness
{
    private static readonly Lazy<IPluginRegistry> Registry = new(() => new PluginLoader(NullLogger<PluginLoader>.Instance).Load(RepoPaths.Plugins));
    private static readonly Lazy<ICheckContext> Context = new(CreateContext);

    /// <summary>Loaded check plugin by id.</summary>
    public static CheckPlugin Plugin(string id)
    {
        Assert.True(Registry.Value.TryGetCheck(id, out var plugin), $"Plugin {id} not loaded.");
        return plugin;
    }

    /// <summary>Binds <paramref name="config"/> through the plugin schema.</summary>
    public static BoundConfig Bind(string id, object config)
    {
        return ConfigBinder.Bind(Plugin(id), JsonSerializer.SerializeToElement(config), "config");
    }

    /// <summary>Binds and probes once.</summary>
    public static Task<ProbeResult> ProbeAsync(string id, object config)
    {
        return Plugin(id).Check.ProbeAsync(Bind(id, config).Value, Context.Value, TestContext.Current.CancellationToken);
    }

    /// <summary>Value of a measurement, failing if the dimension is missing.</summary>
    public static double Measure(this ProbeResult result, string dimension)
    {
        var measurement = result.Measurements.FirstOrDefault(m => m.Dimension == dimension);
        Assert.NotNull(measurement);
        return measurement.Value;
    }

    private static ICheckContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddHttpClient(HostCheckContext.HttpClientName);
        return new HostCheckContext(services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(), TimeProvider.System);
    }
}
