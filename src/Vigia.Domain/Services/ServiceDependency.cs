namespace Vigia.Domain.Services;

/// <summary>
/// An edge in the service graph: <see cref="ServiceId"/> depends on <see cref="DependsOnId"/>.
/// </summary>
public sealed class ServiceDependency
{
    private ServiceDependency()
    {
    }

    /// <summary>Creates an edge.</summary>
    public ServiceDependency(Guid serviceId, Guid dependsOnId, DependencyMode mode)
    {
        ServiceId = serviceId;
        DependsOnId = dependsOnId;
        Mode = mode;
    }

    /// <summary>The dependent service.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>The service it depends on.</summary>
    public Guid DependsOnId { get; private set; }

    /// <summary>How a failure propagates along this edge.</summary>
    public DependencyMode Mode { get; private set; }
}
