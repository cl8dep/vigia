using Vigia.Domain.Common;
using Vigia.Domain.Tags;

namespace Vigia.Domain.StatusPages;

/// <summary>
/// A curated public view of service health. Nothing is public unless a page lists it, under the name the page gives it.
/// </summary>
public sealed class StatusPage : Entity
{
    private readonly List<StatusPageComponent> _components = [];

    private StatusPage()
    {
    }

    /// <summary>Creates a page with no components.</summary>
    /// <exception cref="DomainException">The slug or title is invalid.</exception>
    public StatusPage(string slug, string title, ManagedBy managedBy)
        : base(slug, title, managedBy)
    {
    }

    /// <inheritdoc />
    protected override TaggedEntity TagKind
    {
        get { return TaggedEntity.StatusPage; }
    }

    /// <summary>Optional text under the title.</summary>
    public string? Description { get; private set; }

    /// <summary>Components in display order.</summary>
    public IReadOnlyList<StatusPageComponent> Components
    {
        get { return _components; }
    }

    /// <summary>Sets the description.</summary>
    public void Describe(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>Replaces the components, keeping the given order.</summary>
    /// <exception cref="DomainException">A name is empty or a service appears twice.</exception>
    public void Show(IReadOnlyList<(Guid ServiceId, string Name, string? Group)> components)
    {
        if (components.Any(c => string.IsNullOrWhiteSpace(c.Name)))
        {
            throw new DomainException("Every component needs a public name.");
        }

        if (components.GroupBy(c => c.ServiceId).Any(g => g.Count() > 1))
        {
            throw new DomainException("A service can appear only once on a page.");
        }

        _components.Clear();
        _components.AddRange(components.Select((c, i) => new StatusPageComponent(Id, c.ServiceId, c.Name.Trim(), string.IsNullOrWhiteSpace(c.Group) ? null : c.Group.Trim(), i)));
    }
}
