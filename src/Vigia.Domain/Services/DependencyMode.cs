namespace Vigia.Domain.Services;

/// <summary>
/// How a failing dependency affects the service that depends on it.
/// </summary>
public enum DependencyMode
{
    /// <summary>The dependent is down too; its consequence alerts belong to the cause.</summary>
    Blocking,

    /// <summary>The dependent is degraded.</summary>
    Soft,

    /// <summary>Shown for context only.</summary>
    Advisory,
}
