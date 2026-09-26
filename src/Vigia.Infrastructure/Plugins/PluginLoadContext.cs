using System.Reflection;
using System.Runtime.Loader;

namespace Vigia.Infrastructure.Plugins;

/// <summary>
/// Isolated load context for one plugin. The plugin gets its own dependencies, while the SDK
/// is always resolved from the host so contract types are shared.
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    /// <summary>Assemblies that must always come from the host.</summary>
    public static readonly IReadOnlySet<string> SharedAssemblies = new HashSet<string> { "Vigia.Plugins.Abstractions" };

    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>Creates a collectible context for the plugin whose entry assembly is <paramref name="entryPath"/>.</summary>
    public PluginLoadContext(string entryPath)
        : base(name: Path.GetFileNameWithoutExtension(entryPath), isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(entryPath);
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is not null && SharedAssemblies.Contains(assemblyName.Name))
        {
            return null;
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
