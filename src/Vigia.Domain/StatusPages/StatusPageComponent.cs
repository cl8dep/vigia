namespace Vigia.Domain.StatusPages;

/// <summary>
/// One entry on a status page: a service shown under a public name, optionally in a group.
/// </summary>
public sealed class StatusPageComponent
{
    private StatusPageComponent()
    {
    }

    /// <summary>Creates a component.</summary>
    public StatusPageComponent(Guid statusPageId, Guid serviceId, string name, string? group, int order)
    {
        StatusPageId = statusPageId;
        ServiceId = serviceId;
        Name = name;
        Group = group;
        Order = order;
    }

    /// <summary>Page it belongs to.</summary>
    public Guid StatusPageId { get; private set; }

    /// <summary>Service whose health it shows.</summary>
    public Guid ServiceId { get; private set; }

    /// <summary>Public name; internal names never leave the page's configuration.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Public group heading, or null.</summary>
    public string? Group { get; private set; }

    /// <summary>Position on the page.</summary>
    public int Order { get; private set; }
}
