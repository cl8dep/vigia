using Vigia.Domain.Checks;

namespace Vigia.Application.Workers;

/// <summary>
/// A check a worker must run: everything needed to probe it without asking the control plane.
/// </summary>
/// <param name="CheckId">Check id.</param>
/// <param name="Slug">Check slug, for logs.</param>
/// <param name="Plugin">Check plugin id.</param>
/// <param name="ConfigJson">Validated plugin config.</param>
/// <param name="Interval">Time between probes.</param>
/// <param name="Version">Changes whenever the check changes; used to detect updates.</param>
public sealed record CheckAssignment(Guid CheckId, string Slug, string Plugin, string ConfigJson, TimeSpan Interval, DateTimeOffset Version)
{
    /// <summary>Builds an assignment from a check entity.</summary>
    public static CheckAssignment From(Check check)
    {
        return new CheckAssignment(check.Id, check.Slug, check.Plugin, check.ConfigJson, check.Interval, check.UpdatedAt);
    }
}
