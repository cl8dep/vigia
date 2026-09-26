namespace Vigia.Domain.Tags;

/// <summary>
/// Who assigns a system tag to an entity. Vigia always owns the key and its meaning.
/// </summary>
public enum TagAssignment
{
    /// <summary>Derived from a fact on the entity; users cannot set or remove it.</summary>
    Reconciled,

    /// <summary>Users add or remove it on the entities they choose; the fact is their choice.</summary>
    Assignable,

    /// <summary>Evaluated when read, never stored; users cannot set it.</summary>
    Computed,
}
