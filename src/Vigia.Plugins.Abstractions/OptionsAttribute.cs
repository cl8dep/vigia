namespace Vigia.Plugins;

/// <summary>
/// Restricts a string field to a fixed set of values.
/// </summary>
/// <param name="values">Allowed values.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionsAttribute(params string[] values) : Attribute
{
    /// <summary>Allowed values.</summary>
    public string[] Values { get; } = values;
}
