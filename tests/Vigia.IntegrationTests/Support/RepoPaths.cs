namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Locates folders in the repository from the test output directory.
/// </summary>
public static class RepoPaths
{
    /// <summary>Repository root (folder containing <c>Vigia.slnx</c>).</summary>
    public static string Root { get; } = FindRoot();

    /// <summary>Staged built-in plugins, produced by building the plugin projects.</summary>
    public static string Plugins
    {
        get { return Path.Combine(Root, "artifacts", "plugins"); }
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Vigia.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
