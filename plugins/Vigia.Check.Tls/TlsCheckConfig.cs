using System.ComponentModel.DataAnnotations;
using Vigia.Plugins;

namespace Vigia.Check.Tls;

/// <summary>
/// Config for <see cref="TlsCheck"/>.
/// </summary>
public sealed record TlsCheckConfig
{
    [Field("Host", Placeholder = "example.com")]
    [Required]
    public string Host { get; init; } = string.Empty;

    [Field("Port")]
    [Range(1, 65535)]
    public int Port { get; init; } = 443;

    [Field("Server name (SNI)", Help = "Name sent in the handshake and checked against the certificate. Empty = host.")]
    public string? ServerName { get; init; }

    [Field("Validate chain", Help = "Down when the certificate is not trusted or does not match the name. Turn off for self-signed certificates to only track expiry.")]
    public bool ValidateChain { get; init; } = true;

    [Field("Timeout")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}
