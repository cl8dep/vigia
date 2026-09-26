namespace Vigia.Plugins;

/// <summary>
/// Marks a config property as a field and describes how it is presented.
/// </summary>
/// <param name="label">Label shown in the UI.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FieldAttribute(string label) : Attribute
{
    /// <summary>Label shown in the UI.</summary>
    public string Label { get; } = label;

    /// <summary>Example value shown in an empty input.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Help text shown under the field and in editor tooltips.</summary>
    public string? Help { get; init; }
}
