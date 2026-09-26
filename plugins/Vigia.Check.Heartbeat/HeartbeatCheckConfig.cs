using Vigia.Plugins;

namespace Vigia.Check.Heartbeat;

/// <summary>
/// Config for <see cref="HeartbeatCheck"/>.
/// </summary>
public sealed record HeartbeatCheckConfig
{
    [Field("Expected every", Help = "How often the job pings when healthy, for example 5m for a job that runs every five minutes.")]
    public TimeSpan Every { get; init; } = TimeSpan.FromMinutes(5);

    [Field("Grace", Help = "Extra time allowed after a missed ping before the check goes down.")]
    public TimeSpan Grace { get; init; } = TimeSpan.FromMinutes(1);
}
