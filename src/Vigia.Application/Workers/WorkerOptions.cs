namespace Vigia.Application.Workers;

/// <summary>
/// Worker runtime settings, bound from the <c>Worker</c> config section.
/// </summary>
public sealed class WorkerOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Worker";

    /// <summary>Name recorded on every result this worker produces.</summary>
    public string Name { get; set; } = "builtin";

    /// <summary>Run the built-in worker inside the control plane. Turn off when only remote workers should probe.</summary>
    public bool BuiltInEnabled { get; set; } = true;

    /// <summary>Maximum probes running at the same time.</summary>
    public int MaxConcurrency { get; set; } = 50;

    /// <summary>How often the worker reloads its assignments.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Upper bound for the random delay before a new check's first probe, so checks created together
    /// do not probe in lockstep. Never larger than the check interval.
    /// </summary>
    public TimeSpan InitialJitter { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Upper bound for a single probe.</summary>
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
