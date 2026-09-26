namespace Vigia.Plugins;

/// <summary>
/// Declares a measurement a check produces, so alert rules can target it without knowing the plugin.
/// </summary>
/// <param name="Name">Dimension name, lowercase kebab case, for example <c>days-to-expiry</c>.</param>
/// <param name="Direction">Which way the value gets worse.</param>
/// <param name="Unit">Unit shown in the UI, for example <c>ms</c>.</param>
public sealed record DimensionSpec(string Name, Direction Direction, string? Unit = null)
{
    /// <summary>Response time in milliseconds.</summary>
    public static readonly DimensionSpec Latency = new("latency", Direction.HigherIsWorse, "ms");

    /// <summary>Creates a measurement for this dimension.</summary>
    public Measurement Measure(double value)
    {
        return new(Name, value);
    }
}
