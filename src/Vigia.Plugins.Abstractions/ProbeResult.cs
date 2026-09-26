namespace Vigia.Plugins;

/// <summary>
/// What a probe returns: outcome, measurements and an optional message.
/// </summary>
/// <param name="Outcome">Whether the target is up, down, or the probe failed to run.</param>
/// <param name="Measurements">Values for the dimensions the check declares.</param>
/// <param name="Message">Why the target is down or the probe failed. Shown to users and to the investigation agent.</param>
public sealed record ProbeResult(Outcome Outcome, IReadOnlyList<Measurement> Measurements, string? Message = null)
{
    /// <summary>The target is up.</summary>
    public static ProbeResult Up(params Measurement[] measurements)
    {
        return new(Outcome.Up, measurements);
    }

    /// <summary>The target is down.</summary>
    public static ProbeResult Down(string message, params Measurement[] measurements)
    {
        return new(Outcome.Down, measurements, message);
    }

    /// <summary>The probe could not run.</summary>
    public static ProbeResult Error(string message)
    {
        return new(Outcome.Error, [], message);
    }
}
