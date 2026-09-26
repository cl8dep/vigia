namespace Vigia.Application.Agents;

/// <summary>
/// Agent runtime settings, bound from the <c>Agent</c> config section.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Agent";

    /// <summary>Name recorded on every result this agent produces.</summary>
    public string Name { get; set; } = "builtin";

    /// <summary>Run the built-in agent inside the control plane. Turn off when only remote agents should probe.</summary>
    public bool BuiltInEnabled { get; set; } = true;

    /// <summary>Maximum probes running at the same time.</summary>
    public int MaxConcurrency { get; set; } = 50;

    /// <summary>How often the agent reloads its assignments.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Upper bound for the random delay before a new check's first probe, so checks created together
    /// do not probe in lockstep. Never larger than the check interval.
    /// </summary>
    public TimeSpan InitialJitter { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Upper bound for a single probe.</summary>
    public TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
