namespace Vigia.Plugins;

/// <summary>
/// Shows a field only when another field has one of the given values.
/// </summary>
/// <param name="field">Name of the controlling property.</param>
/// <param name="values">Values of the controlling field that make this field visible.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VisibleWhenAttribute(string field, params string[] values) : Attribute
{
    /// <summary>Name of the controlling property.</summary>
    public string Field { get; } = field;

    /// <summary>Values of the controlling field that make this field visible.</summary>
    public string[] Values { get; } = values;
}
