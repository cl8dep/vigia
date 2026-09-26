namespace Vigia.Plugins;

/// <summary>
/// Services the host provides to a check while it probes.
/// </summary>
public interface ICheckContext
{
    /// <summary>Creates an HTTP client managed by the host (pooling, proxy, SSRF policy).</summary>
    HttpClient CreateHttpClient();

    /// <summary>Clock to use instead of <see cref="DateTime.UtcNow"/>, so probes are testable.</summary>
    TimeProvider Time { get; }
}
