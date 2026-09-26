using System.ComponentModel.DataAnnotations;
using Vigia.Plugins;

namespace Vigia.Check.Http;

/// <summary>
/// Config for <see cref="HttpCheck"/>.
/// </summary>
public sealed record HttpCheckConfig
{
    [Field("URL", Placeholder = "https://example.com/health")]
    [Required, Url]
    public string Url { get; init; } = string.Empty;

    [Field("Method")]
    [Options("GET", "HEAD", "POST")]
    public string Method { get; init; } = "GET";

    [Field("Expected status codes", Help = "The check is down when the response status is not in this list.")]
    public List<int> ExpectedStatus { get; init; } = [200];

    [Field("Timeout")]
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    [Field("Headers", Help = "Request headers, for example Authorization.")]
    public Dictionary<string, string>? Headers { get; init; }

    [Field("Body contains", Help = "Optional text the response body must contain.")]
    public string? BodyContains { get; init; }
}
