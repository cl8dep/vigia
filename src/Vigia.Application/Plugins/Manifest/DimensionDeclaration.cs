namespace Vigia.Application.Plugins.Manifest;

/// <summary>
/// A measurement a check declares in <c>check.dimensions</c>. Alert rules can only target declared dimensions.
/// </summary>
/// <param name="Name">Lowercase kebab case, for example <c>days-to-expiry</c>.</param>
/// <param name="Direction">Which way the value gets worse.</param>
/// <param name="Unit">Display unit, for example <c>ms</c>.</param>
public sealed record DimensionDeclaration(string Name, DimensionDirection Direction, string? Unit);
