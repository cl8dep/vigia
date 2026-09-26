namespace Vigia.Application.Plugins;

/// <summary>
/// Condition for showing a field.
/// </summary>
/// <param name="Field">JSON name of the controlling field.</param>
/// <param name="Values">Values of the controlling field that make the field visible.</param>
public sealed record VisibleWhen(string Field, IReadOnlyList<string> Values);
