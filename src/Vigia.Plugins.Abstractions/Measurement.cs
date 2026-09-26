namespace Vigia.Plugins;

/// <summary>
/// One value measured by a probe for a declared dimension.
/// </summary>
/// <param name="Dimension">Name of a <see cref="DimensionSpec"/> declared in the manifest.</param>
/// <param name="Value">Measured value, in the dimension's unit.</param>
public sealed record Measurement(string Dimension, double Value);
