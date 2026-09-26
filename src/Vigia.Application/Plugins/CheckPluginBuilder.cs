using System.Reflection;
using Vigia.Application.Plugins.Manifest;
using Vigia.Domain.Common;
using Vigia.Plugins;

namespace Vigia.Application.Plugins;

/// <summary>
/// Instantiates the components a manifest declares, like Android does with <c>AndroidManifest.xml</c>:
/// classes are looked up by name in the plugin's own assembly only, and nothing undeclared is ever created.
/// </summary>
public static class CheckPluginBuilder
{
    /// <summary>Validates the manifest against <paramref name="assembly"/> and builds the plugin.</summary>
    /// <exception cref="InvalidOperationException">A declared class is missing or of the wrong kind, or the manifest is inconsistent.</exception>
    public static CheckPlugin Build(PluginManifest manifest, Assembly assembly)
    {
        var id = manifest.Id;
        var configType = Resolve(assembly, manifest.Check.Config, id, "check.config");
        var check = Instantiate<ICheck>(assembly, manifest.Check.Class, id, "check.class");

        var typedConfig = FindCheckConfigType(check.GetType());
        if (typedConfig is not null && typedConfig != configType)
        {
            throw new InvalidOperationException(
                $"Plugin '{id}': check.class {check.GetType().Name} is Check<{typedConfig.Name}> but check.config is {configType.Name}.");
        }

        EnsureNames(id, "dimension", (manifest.Check.Dimensions ?? []).Select(d => d.Name).ToList());

        var webhookDeclarations = manifest.Webhooks ?? [];
        EnsureNames(id, "webhook", webhookDeclarations.Select(w => w.Name).ToList());
        var webhooks = webhookDeclarations.ToDictionary(
            w => w.Name,
            w => Instantiate<IWebhookHandler>(assembly, w.Class, id, $"webhooks[{w.Name}].class"));

        return new CheckPlugin(manifest, check, configType, ConfigSchemaBuilder.Build(configType), webhooks);
    }

    private static Type Resolve(Assembly assembly, string typeName, string id, string field)
    {
        return assembly.GetType(typeName, throwOnError: false)
            ?? throw new InvalidOperationException($"Plugin '{id}': {field} '{typeName}' was not found in {assembly.GetName().Name}.");
    }

    private static T Instantiate<T>(Assembly assembly, string typeName, string id, string field) where T : class
    {
        var type = Resolve(assembly, typeName, id, field);
        if (!typeof(T).IsAssignableFrom(type) || type.IsAbstract)
        {
            throw new InvalidOperationException($"Plugin '{id}': {field} '{typeName}' does not implement {typeof(T).Name}.");
        }

        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException($"Plugin '{id}': {field} '{typeName}' needs a public parameterless constructor.");
        }

        return (T)Activator.CreateInstance(type)!;
    }

    private static Type? FindCheckConfigType(Type checkType)
    {
        for (var type = checkType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Check<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static void EnsureNames(string id, string kind, IReadOnlyList<string> names)
    {
        var invalid = names.Where(n => !Slug.IsValid(n)).ToList();
        if (invalid.Count > 0)
        {
            throw new InvalidOperationException($"Plugin '{id}' declares invalid {kind} name(s) {string.Join(", ", invalid)}. Use lowercase kebab case.");
        }

        var duplicated = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicated.Count > 0)
        {
            throw new InvalidOperationException($"Plugin '{id}' declares {kind}(s) {string.Join(", ", duplicated)} more than once.");
        }
    }
}
