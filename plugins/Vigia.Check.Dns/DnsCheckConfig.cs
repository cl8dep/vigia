using System.ComponentModel.DataAnnotations;
using Vigia.Plugins;

namespace Vigia.Check.Dns;

/// <summary>
/// Config for <see cref="DnsCheck"/>.
/// </summary>
public sealed record DnsCheckConfig
{
    [Field("Host", Placeholder = "api.example.com")]
    [Required]
    public string Host { get; init; } = string.Empty;

    [Field("Record type")]
    [Options("A", "AAAA", "CNAME", "MX", "TXT", "NS")]
    public string RecordType { get; init; } = "A";

    [Field("Resolvers", Placeholder = "1.1.1.1", Help = "IP or IP:port of each resolver to query. Empty = the worker's system resolvers. With several, each one is checked on its own.")]
    public List<string> Resolvers { get; init; } = [];

    [Field("Expected values", Help = "Every value must be in the answer of every resolver. Empty = any non-empty answer is up.")]
    public List<string> Expected { get; init; } = [];

    [Field("Timeout", Help = "Per resolver.")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
}
