using System.Text.Json;

namespace Vigia.Infrastructure.Plugins;

/// <summary>
/// Contents of <c>plugin.json</c>. Read before any plugin code is loaded.
/// </summary>
/// <param name="Id">Plugin id; must match the folder name and the id the plugin reports.</param>
/// <param name="Version">Plugin version; must match the version folder.</param>
/// <param name="Sdk">Supported SDK major, for example <c>1.x</c>.</param>
/// <param name="Entry">Entry assembly file name.</param>
/// <param name="Kinds">Plugin kinds provided, for example <c>check</c>.</param>
public sealed record PluginManifestFile(string Id, string Version, string Sdk, string Entry, IReadOnlyList<string> Kinds)
{
    /// <summary>File name of the manifest inside a plugin version folder.</summary>
    public const string FileName = "plugin.json";

    /// <summary>Reads and parses a manifest.</summary>
    /// <exception cref="InvalidDataException">The file is not a valid manifest.</exception>
    public static PluginManifestFile Read(string path)
    {
        var manifest = JsonSerializer.Deserialize<PluginManifestFile>(File.ReadAllText(path), JsonSerializerOptions.Web);
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id) || string.IsNullOrWhiteSpace(manifest.Entry))
        {
            throw new InvalidDataException("plugin.json must define id and entry.");
        }

        return manifest;
    }
}
