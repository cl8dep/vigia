using Vigia.Plugins;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Host check context with no services, for tests that only exercise capability gating.
/// </summary>
public sealed class NoServicesContext : ICheckContext
{
    /// <inheritdoc />
    public TimeProvider Time
    {
        get { return TimeProvider.System; }
    }

    /// <inheritdoc />
    public HttpClient CreateHttpClient()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public T GetRequiredService<T>() where T : class
    {
        throw new NotSupportedException();
    }
}
