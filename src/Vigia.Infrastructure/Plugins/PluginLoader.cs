using Microsoft.Extensions.Logging;
using Vigia.Application.Plugins;
using Vigia.Application.Plugins.Manifest;

namespace Vigia.Infrastructure.Plugins;

/// <summary>
/// Discovers and loads plugins from <c>&lt;root&gt;/&lt;id&gt;/&lt;version&gt;/</c>.
/// A plugin that fails any step is recorded as a failure; loading never throws for a bad plugin.
/// </summary>
public sealed class PluginLoader(ILogger<PluginLoader> logger)
{
    /// <summary>SDK major this host supports.</summary>
    public const int SdkMajor = 1;

    /// <summary>Loads every plugin under <paramref name="root"/>, using the highest version per id.</summary>
    public PluginRegistry Load(string root)
    {
        var checks = new List<CheckPlugin>();
        var failures = new List<PluginLoadFailure>();

        if (!Directory.Exists(root))
        {
            logger.LogWarning("Plugin folder {Root} does not exist; no plugins loaded", root);
            return new PluginRegistry(checks, failures);
        }

        foreach (var pluginDir in Directory.EnumerateDirectories(root))
        {
            var versionDir = Directory.EnumerateDirectories(pluginDir)
                .Select(d => (Dir: d, Version: Version.TryParse(Path.GetFileName(d), out var v) ? v : null))
                .Where(d => d.Version is not null)
                .OrderByDescending(d => d.Version)
                .Select(d => d.Dir)
                .FirstOrDefault();

            if (versionDir is null)
            {
                failures.Add(new PluginLoadFailure(pluginDir, "No version folder found."));
                continue;
            }

            try
            {
                checks.Add(LoadOne(versionDir));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load plugin at {Path}", versionDir);
                failures.Add(new PluginLoadFailure(versionDir, ex.Message));
            }
        }

        logger.LogInformation("Loaded {Count} check plugin(s), {Failures} failure(s)", checks.Count, failures.Count);
        return new PluginRegistry(checks, failures);
    }

    private static CheckPlugin LoadOne(string versionDir)
    {
        var manifestPath = Path.Combine(versionDir, PluginManifest.FileName);
        if (!File.Exists(manifestPath))
        {
            throw new InvalidDataException($"Missing {PluginManifest.FileName}.");
        }

        var manifest = PluginManifest.Parse(File.ReadAllText(manifestPath));
        if (manifest.Sdk != $"{SdkMajor}.x")
        {
            throw new InvalidDataException($"Plugin targets SDK {manifest.Sdk}; this host supports {SdkMajor}.x.");
        }

        if (manifest.Id != Path.GetFileName(Path.GetDirectoryName(versionDir)) || manifest.Version != Path.GetFileName(versionDir))
        {
            throw new InvalidDataException("plugin.json id and version must match the folder layout <id>/<version>.");
        }

        var entryPath = Path.Combine(versionDir, manifest.Entry);
        if (!File.Exists(entryPath))
        {
            throw new InvalidDataException($"Entry assembly {manifest.Entry} not found.");
        }

        var assembly = new PluginLoadContext(entryPath).LoadFromAssemblyPath(entryPath);
        return CheckPluginBuilder.Build(manifest, assembly);
    }
}
