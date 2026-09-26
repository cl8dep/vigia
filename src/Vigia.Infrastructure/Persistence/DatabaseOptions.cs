namespace Vigia.Infrastructure.Persistence;

/// <summary>
/// Database settings, bound from the <c>Database</c> config section.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Database";

    /// <summary>
    /// Apply pending migrations at startup. On by default so a single container just works;
    /// turn off when migrations run as a separate deployment step.
    /// </summary>
    public bool MigrateOnStartup { get; set; } = true;
}
