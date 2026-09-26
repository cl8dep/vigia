using Microsoft.Extensions.Logging.Abstractions;
using Vigia.Application.Plugins;
using Vigia.Infrastructure.Plugins;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Plugins;

/// <summary>
/// Loads the real built-in plugins from <c>artifacts/plugins</c>.
/// </summary>
public sealed class PluginLoaderTests
{
    private readonly PluginLoader _loader = new(NullLogger<PluginLoader>.Instance);

    [Fact]
    public void Loads_http_plugin_from_folder()
    {
        var registry = _loader.Load(RepoPaths.Plugins);

        Assert.Empty(registry.Failures);
        Assert.True(registry.TryGetCheck("vigia.check.http", out var plugin));
        Assert.Equal("1.0.0", plugin.Version);
        Assert.Equal("HTTP", plugin.Manifest.Label);
    }

    [Fact]
    public void Loads_every_built_in_plugin()
    {
        var registry = _loader.Load(RepoPaths.Plugins);

        Assert.Empty(registry.Failures);
        Assert.Equal(
            ["vigia.check.dns", "vigia.check.heartbeat", "vigia.check.http", "vigia.check.ping", "vigia.check.tcp", "vigia.check.tls"],
            registry.Checks.Select(c => c.Id).Order());
    }

    [Fact]
    public void Plugin_dependencies_load_inside_the_plugin_context()
    {
        var registry = _loader.Load(RepoPaths.Plugins);
        registry.TryGetCheck("vigia.check.dns", out var dns);
        var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(dns.Check.GetType().Assembly)!;

        // Dependencies load lazily, on first use; resolve it the way the runtime would.
        var dnsClient = context.LoadFromAssemblyName(new System.Reflection.AssemblyName("DnsClient"));

        Assert.Same(context, System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(dnsClient));
        Assert.StartsWith(Path.Combine(RepoPaths.Plugins, "vigia.check.dns"), dnsClient.Location);
        Assert.DoesNotContain(System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies, a => a.GetName().Name == "DnsClient");
    }

    [Fact]
    public void Plugin_is_isolated_but_shares_sdk_types_with_host()
    {
        var registry = _loader.Load(RepoPaths.Plugins);
        registry.TryGetCheck("vigia.check.http", out var plugin);

        var pluginContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(plugin.Check.GetType().Assembly);
        Assert.IsType<PluginLoadContext>(pluginContext);
        Assert.Same(typeof(ICheck).Assembly, plugin.Check.GetType().GetInterface(nameof(ICheck))!.Assembly);
    }

    [Fact]
    public void Builds_schema_from_config_attributes()
    {
        var registry = _loader.Load(RepoPaths.Plugins);
        registry.TryGetCheck("vigia.check.http", out var plugin);
        var fields = plugin.Schema.Fields.ToDictionary(f => f.Name);

        Assert.True(fields["url"].Required);
        Assert.Equal(ConfigFieldType.String, fields["url"].Type);
        Assert.Equal(["GET", "HEAD", "POST"], fields["method"].Options);
        Assert.Equal(ConfigFieldType.List, fields["expectedStatus"].Type);
        Assert.Equal(ConfigFieldType.Integer, fields["expectedStatus"].ItemType);
        Assert.Equal("[200]", fields["expectedStatus"].Default?.GetRawText());
        Assert.Equal(ConfigFieldType.Duration, fields["timeout"].Type);
        Assert.Equal("\"10s\"", fields["timeout"].Default?.GetRawText());
        Assert.Equal(ConfigFieldType.Map, fields["headers"].Type);
    }

    [Fact]
    public void Broken_plugin_is_reported_and_does_not_stop_the_others()
    {
        var root = Directory.CreateTempSubdirectory("vigia-plugins-").FullName;
        try
        {
            CopyDirectory(Path.Combine(RepoPaths.Plugins, "vigia.check.http"), Path.Combine(root, "vigia.check.http"));
            var broken = Directory.CreateDirectory(Path.Combine(root, "vigia.check.broken", "1.0.0")).FullName;
            File.WriteAllText(Path.Combine(broken, "plugin.json"), """
                { "id": "vigia.check.broken", "version": "1.0.0", "sdk": "1.x", "entry": "Missing.dll", "label": "Broken", "description": "",
                  "check": { "class": "Broken.Check", "config": "Broken.Config" } }
                """);

            var registry = _loader.Load(root);

            Assert.True(registry.TryGetCheck("vigia.check.http", out _));
            var failure = Assert.Single(registry.Failures);
            Assert.Contains("Missing.dll", failure.Reason);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Missing_plugin_folder_loads_nothing()
    {
        var registry = _loader.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.Empty(registry.Checks);
        Assert.Empty(registry.Failures);
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest);
        }
    }
}
