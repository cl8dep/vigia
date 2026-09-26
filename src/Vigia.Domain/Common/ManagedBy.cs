namespace Vigia.Domain.Common;

/// <summary>
/// Which front end owns an entity. Entities owned by one front end are read-only for the others.
/// </summary>
public enum ManagedBy
{
    /// <summary>Created and edited in the admin UI or directly through the API.</summary>
    Ui,

    /// <summary>Owned by <c>vigia.yaml</c> and applied with the CLI.</summary>
    Yaml,

    /// <summary>Owned by the Terraform provider.</summary>
    Terraform,
}
