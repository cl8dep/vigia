using Vigia.Plugins;

namespace Vigia.Infrastructure.Checks;

/// <summary>
/// <see cref="ICheckContext"/> backed by host services.
/// </summary>
public sealed class HostCheckContext(IHttpClientFactory httpClients, TimeProvider time) : ICheckContext
{
    /// <summary>Name of the HTTP client configuration used for probes.</summary>
    public const string HttpClientName = "probes";

    /// <inheritdoc />
    public TimeProvider Time { get; } = time;

    /// <inheritdoc />
    public HttpClient CreateHttpClient()
    {
        return httpClients.CreateClient(HttpClientName);
    }

    /// <inheritdoc />
    /// <remarks>Capability-gated services are bound to a check; they exist only in the per-probe context.</remarks>
    public T GetRequiredService<T>() where T : class
    {
        throw new InvalidOperationException($"{typeof(T).Name} is only available while probing a specific check.");
    }
}
