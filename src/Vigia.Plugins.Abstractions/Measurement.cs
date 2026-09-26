namespace Vigia.Plugins;

/// <summary>
/// One value measured by a probe.
/// </summary>
/// <param name="Dimension">Dimension name declared in the manifest (<c>check.dimensions</c>). Undeclared names make the probe an error.</param>
/// <param name="Value">Measured value, in the dimension's declared unit.</param>
public sealed record Measurement(string Dimension, double Value)
{
    /// <summary>Response time in milliseconds, dimension <c>latency</c>.</summary>
    public static Measurement Latency(double milliseconds)
    {
        return new Measurement("latency", milliseconds);
    }
}
