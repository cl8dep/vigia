namespace Vigia.Infrastructure.Plugins;

/// <summary>
/// Plugin loading settings, bound from the <c>Plugins</c> config section.
/// </summary>
public sealed class PluginOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Plugins";

    /// <summary>Folder containing <c>&lt;id&gt;/&lt;version&gt;/plugin.json</c>. Relative paths resolve from the app base directory.</summary>
    public string Path { get; set; } = "plugins";
}
