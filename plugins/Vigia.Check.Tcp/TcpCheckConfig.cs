using System.ComponentModel.DataAnnotations;
using Vigia.Plugins;

namespace Vigia.Check.Tcp;

/// <summary>
/// Config for <see cref="TcpCheck"/>.
/// </summary>
public sealed record TcpCheckConfig
{
    [Field("Host", Placeholder = "db.internal")]
    [Required]
    public string Host { get; init; } = string.Empty;

    [Field("Port", Placeholder = "5432")]
    [Range(1, 65535)]
    public int Port { get; init; }

    [Field("Timeout")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
