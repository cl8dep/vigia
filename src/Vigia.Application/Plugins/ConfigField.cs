using System.Text.Json;

namespace Vigia.Application.Plugins;

/// <summary>
/// One field of a plugin config schema.
/// </summary>
/// <param name="Name">JSON name (camelCase).</param>
/// <param name="Type">Value type.</param>
/// <param name="Label">UI label.</param>
/// <param name="Required">Whether a value must be provided.</param>
/// <param name="Secret">Encrypted at rest and never returned by the API.</param>
/// <param name="ItemType">Element type when <paramref name="Type"/> is <see cref="ConfigFieldType.List"/>.</param>
/// <param name="Options">Allowed values, when restricted.</param>
/// <param name="VisibleWhen">Controlling field and values that make this field visible.</param>
/// <param name="Default">Default value, as JSON.</param>
/// <param name="Placeholder">Example shown in an empty input.</param>
/// <param name="Help">Help text.</param>
public sealed record ConfigField(
    string Name,
    ConfigFieldType Type,
    string Label,
    bool Required,
    bool Secret,
    ConfigFieldType? ItemType,
    IReadOnlyList<string>? Options,
    VisibleWhen? VisibleWhen,
    JsonElement? Default,
    string? Placeholder,
    string? Help);
