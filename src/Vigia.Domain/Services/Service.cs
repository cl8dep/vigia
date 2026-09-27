using Vigia.Domain.Common;
using Vigia.Domain.Tags;

namespace Vigia.Domain.Services;

/// <summary>
/// Something users care about, built from checks selected by tags and connected to other services by dependencies.
/// Its health is derived from the alerts on its checks; see <c>docs/services.md</c>.
/// </summary>
/// <remarks>
/// A third-party provider carries the <c>vigia:external</c> tag. Services select checks; checks never point to services,
/// so one check can feed several services.
/// </remarks>
public sealed class Service : Entity
{
    private readonly List<ServiceDependency> _dependencies = [];

    private Service()
    {
    }

    /// <summary>Creates a service with no checks and no dependencies.</summary>
    /// <exception cref="DomainException">The slug or name is invalid.</exception>
    public Service(string slug, string name, ManagedBy managedBy)
        : base(slug, name, managedBy)
    {
    }

    /// <inheritdoc />
    protected override TaggedEntity TagKind
    {
        get { return TaggedEntity.Service; }
    }

    /// <summary>Tags a check must have to belong to this service, or null for no checks.</summary>
    public TagSelector? Checks { get; private set; }

    /// <summary>
    /// Check tag key that splits the service into partitions (usually <c>region</c>), so one partition down is a
    /// partial outage rather than a full one. Null for an unpartitioned service.
    /// </summary>
    public string? PartitionBy { get; private set; }

    /// <summary>Services this one depends on.</summary>
    public IReadOnlyList<ServiceDependency> Dependencies
    {
        get { return _dependencies; }
    }

    /// <summary>Sets which checks belong to the service and how they partition.</summary>
    /// <exception cref="DomainException">The partition key is not a valid tag key.</exception>
    public void Select(TagSelector? checks, string? partitionBy)
    {
        if (partitionBy is not null)
        {
            TagRules.ValidateSelectorKey(partitionBy);
        }

        Checks = checks is { IsEmpty: true } ? null : checks;
        PartitionBy = partitionBy;
    }

    /// <summary>Replaces the dependencies. Cycle detection needs the whole graph and happens before this call.</summary>
    /// <exception cref="DomainException">The service depends on itself or on the same service twice.</exception>
    public void DependOn(IReadOnlyList<(Guid ServiceId, DependencyMode Mode)> dependencies)
    {
        if (dependencies.Any(d => d.ServiceId == Id))
        {
            throw new DomainException("A service cannot depend on itself.");
        }

        if (dependencies.GroupBy(d => d.ServiceId).Any(g => g.Count() > 1))
        {
            throw new DomainException("Each dependency can appear only once.");
        }

        _dependencies.Clear();
        _dependencies.AddRange(dependencies.Select(d => new ServiceDependency(Id, d.ServiceId, d.Mode)));
    }
}
