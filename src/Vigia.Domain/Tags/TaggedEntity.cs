namespace Vigia.Domain.Tags;

/// <summary>
/// Kinds of entity that carry tags, so system tags can declare where they apply.
/// </summary>
public enum TaggedEntity
{
    Check,
    Worker,
    Rule,
    Service,
}
