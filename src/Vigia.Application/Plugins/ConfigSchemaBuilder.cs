using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Vigia.Application.Common.Json;
using Vigia.Plugins;

namespace Vigia.Application.Plugins;

/// <summary>
/// Builds a <see cref="ConfigSchema"/> from a plugin config record through reflection over its properties and attributes.
/// </summary>
public static class ConfigSchemaBuilder
{
    /// <summary>Builds the schema for <paramref name="configType"/>.</summary>
    /// <exception cref="InvalidOperationException">The config type has no parameterless constructor or uses an unsupported property type.</exception>
    public static ConfigSchema Build(Type configType)
    {
        var defaults = Activator.CreateInstance(configType)
            ?? throw new InvalidOperationException($"{configType.Name} needs a parameterless constructor.");

        var fields = configType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => BuildField(p, defaults))
            .ToList();

        return new ConfigSchema(fields);
    }

    private static ConfigField BuildField(PropertyInfo property, object defaults)
    {
        var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        var field = property.GetCustomAttribute<FieldAttribute>();
        var visibleWhen = property.GetCustomAttribute<VisibleWhenAttribute>();
        var (type, itemType) = MapType(property.PropertyType, property);
        var value = property.GetValue(defaults);

        return new ConfigField(
            Name: name,
            Type: type,
            Label: field?.Label ?? property.Name,
            Required: property.GetCustomAttribute<RequiredAttribute>() is not null,
            Secret: property.GetCustomAttribute<SecretAttribute>() is not null,
            ItemType: itemType,
            Options: property.GetCustomAttribute<OptionsAttribute>()?.Values,
            VisibleWhen: visibleWhen is null
                ? null
                : new VisibleWhen(JsonNamingPolicy.CamelCase.ConvertName(visibleWhen.Field), visibleWhen.Values),
            Default: value is null ? null : JsonSerializer.SerializeToElement(value, property.PropertyType, ConfigJson.Options),
            Placeholder: field?.Placeholder,
            Help: field?.Help);
    }

    private static (ConfigFieldType Type, ConfigFieldType? ItemType) MapType(Type type, PropertyInfo property)
    {
        var scalar = MapScalar(Nullable.GetUnderlyingType(type) ?? type);
        if (scalar is not null)
        {
            return (scalar.Value, null);
        }

        if (type.IsAssignableTo(typeof(IDictionary<string, string>)) || type == typeof(IReadOnlyDictionary<string, string>))
        {
            return (ConfigFieldType.Map, null);
        }

        var element = type.IsArray
            ? type.GetElementType()
            : type.GetInterfaces().Append(type)
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];

        var itemType = element is null ? null : MapScalar(element);
        if (itemType is null)
        {
            throw new InvalidOperationException(
                $"Config property {property.DeclaringType?.Name}.{property.Name} has unsupported type {type.Name}.");
        }

        return (ConfigFieldType.List, itemType);
    }

    private static ConfigFieldType? MapScalar(Type type)
    {
        if (type == typeof(string))
        {
            return ConfigFieldType.String;
        }

        if (type == typeof(int) || type == typeof(long))
        {
            return ConfigFieldType.Integer;
        }

        if (type == typeof(double) || type == typeof(decimal) || type == typeof(float))
        {
            return ConfigFieldType.Number;
        }

        if (type == typeof(bool))
        {
            return ConfigFieldType.Boolean;
        }

        if (type == typeof(TimeSpan))
        {
            return ConfigFieldType.Duration;
        }

        return null;
    }
}
