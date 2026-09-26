using Vigia.Domain.Tags;

namespace Vigia.Domain.Common;

/// <summary>
/// Base for configurable entities: identity, slug, tags, ownership and timestamps.
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

    /// <summary>
    /// Tags: <c>key</c> (value null) or <c>key:value</c>. Includes system tags (<c>vigia:*</c>) derived from the entity.
    /// Used by selectors for routing, worker placement and investigation.
    /// </summary>
    public Dictionary<string, string?> Tags { get; private set; } = [];

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

    /// <summary>Replaces the user tags. System tags are kept.</summary>
    /// <exception cref="DomainException">A tag is invalid or uses the reserved namespace.</exception>
    public void SetTags(IReadOnlyDictionary<string, string?> tags)
    {
        TagRules.ValidateUserTags(tags);
        var system = Tags.Where(t => SystemTags.IsReserved(t.Key));
        Tags = new Dictionary<string, string?>(tags.Concat(system), StringComparer.Ordinal);
        if (Tags.Count > TagRules.MaxTagsPerEntity)
        {
            throw new DomainException($"At most {TagRules.MaxTagsPerEntity} tags per entity, system tags included.");
        }
    }

    /// <summary>Sets a system tag. Only for facts the entity itself derives.</summary>
    protected void SetSystemTag(string key, string? value)
    {
        Tags[key] = value;
    }

    /// <summary>Removes a system tag when the fact it mirrors no longer holds.</summary>
    protected void RemoveSystemTag(string key)
    {
        Tags.Remove(key);
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
