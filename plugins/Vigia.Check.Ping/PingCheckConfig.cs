using System.ComponentModel.DataAnnotations;
using Vigia.Plugins;

namespace Vigia.Check.Ping;

/// <summary>
/// Config for <see cref="PingCheck"/>.
/// </summary>
public sealed record PingCheckConfig
{
    [Field("Host", Placeholder = "10.0.0.1")]
    [Required]
    public string Host { get; init; } = string.Empty;

    [Field("Count", Help = "Echo requests per probe.")]
    [Range(1, 20)]
    public int Count { get; init; } = 3;

    [Field("Timeout", Help = "Per echo request.")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);
}
