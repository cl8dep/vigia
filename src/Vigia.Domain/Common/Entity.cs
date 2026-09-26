namespace Vigia.Domain.Common;

/// <summary>
/// Base for configurable entities: identity, slug, labels, ownership and timestamps.
/// </summary>
public abstract class Entity
{
    /// <summary>For EF Core.</summary>
    protected Entity()
    {
    }

    /// <summary>Creates an entity with a new UUIDv7 id.</summary>
    /// <param name="slug">Stable identity for config as code. See <see cref="Common.Slug"/>.</param>
    /// <param name="name">Display name.</param>
    /// <param name="managedBy">Front end that owns the entity.</param>
    /// <exception cref="DomainException">The slug or name is invalid.</exception>
    protected Entity(string slug, string name, ManagedBy managedBy)
    {
        if (!Common.Slug.IsValid(slug))
        {
            throw new DomainException($"Invalid slug '{slug}'. Use lowercase kebab case, starting with a letter, up to {Common.Slug.MaxLength} characters.");
        }

        Id = Guid.CreateVersion7();
        Slug = slug;
        Rename(name);
        ManagedBy = managedBy;
    }

    /// <summary>Internal id. Never exposed as config identity; use <see cref="Slug"/>.</summary>
    public Guid Id { get; private set; }

    /// <summary>Stable identity used by YAML, Terraform and the API. Immutable.</summary>
    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Free-form <c>key=value</c> labels used by selectors (routing, worker placement, investigation).</summary>
    public Dictionary<string, string> Labels { get; private set; } = [];

    /// <summary>Front end that owns this entity.</summary>
    public ManagedBy ManagedBy { get; private set; }

    /// <summary>Set by persistence on insert.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Set by persistence on every update.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Changes the display name.</summary>
    /// <exception cref="DomainException">The name is empty.</exception>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        Name = name.Trim();
    }

    /// <summary>Replaces all labels.</summary>
    public void SetLabels(IReadOnlyDictionary<string, string> labels)
    {
        Labels = new Dictionary<string, string>(labels);
    }

    /// <summary>Called by persistence; not for use in application code.</summary>
    public void Touch(DateTimeOffset now, bool isNew)
    {
        if (isNew)
        {
            CreatedAt = now;
        }

        UpdatedAt = now;
    }
}
