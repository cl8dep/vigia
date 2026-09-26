using System.Text.Json;

namespace Vigia.Application.Plugins;

/// <summary>
/// Plugin config after binding and validation.
/// </summary>
/// <param name="Value">Typed config instance, ready for the plugin.</param>
/// <param name="Normalized">Config serialized back with defaults filled in. This is what gets stored.</param>
public sealed record BoundConfig(object Value, JsonElement Normalized);
