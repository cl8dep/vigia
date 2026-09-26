namespace Vigia.Application.Plugins.ListPlugins;

/// <summary>
/// A dimension a check plugin measures.
/// </summary>
/// <param name="Name">Dimension name used by alert rules.</param>
/// <param name="Direction"><c>higherIsWorse</c> or <c>lowerIsWorse</c>.</param>
/// <param name="Unit">Display unit.</param>
public sealed record DimensionDto(string Name, string Direction, string? Unit);
