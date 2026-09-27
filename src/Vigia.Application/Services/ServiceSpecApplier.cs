using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Json;
using Vigia.Domain.Common;
using Vigia.Domain.Services;

namespace Vigia.Application.Services;

/// <summary>
/// Validates a <see cref="ServiceSpec"/> against the rest of the graph and applies it.
/// </summary>
public static class ServiceSpecApplier
{
    /// <summary>Applies <paramref name="spec"/> to <paramref name="service"/>.</summary>
    /// <exception cref="ValidationException">A field is invalid, a dependency is unknown, or a cycle would be created.</exception>
    public static async Task ApplyAsync(Service service, string slug, ServiceSpec spec, IAppDbContext db, CancellationToken ct)
    {
        Apply("name", () => service.Rename(spec.Name ?? slug));
        Apply("tags", () => service.SetTags(spec.Tags ?? new Dictionary<string, string?>()));

        var checks = spec.Checks is { Count: > 0 } selector ? TagSelectorJson.Parse(selector, "checks") : null;
        Apply("partitionBy", () => service.Select(checks, spec.PartitionBy));

        var requested = spec.DependsOn ?? [];
        var targetSlugs = requested.Select(d => d.Service).Distinct().ToList();
        var targets = await db.Services.AsNoTracking().Where(s => targetSlugs.Contains(s.Slug)).ToDictionaryAsync(s => s.Slug, s => s.Id, ct);
        var missing = targetSlugs.Where(s => !targets.ContainsKey(s)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException("dependsOn", $"Unknown service(s): {string.Join(", ", missing)}.");
        }

        var dependencies = requested.Select(d => (targets[d.Service], ParseMode(d))).ToList();

        var slugs = await db.Services.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Slug, ct);
        slugs[service.Id] = slug;
        var otherEdges = await db.ServiceDependencies.AsNoTracking()
            .Where(d => d.ServiceId != service.Id)
            .Select(d => new { d.ServiceId, d.DependsOnId })
            .ToListAsync(ct);
        DependencyGraph.EnsureAcyclic(service.Id, dependencies.Select(d => d.Item1), otherEdges.Select(e => (e.ServiceId, e.DependsOnId)), slugs);

        Apply("dependsOn", () => service.DependOn(dependencies));
    }

    private static DependencyMode ParseMode(DependencySpec dependency)
    {
        if (dependency.Mode is null)
        {
            return DependencyMode.Blocking;
        }

        if (!Enum.TryParse<DependencyMode>(dependency.Mode, ignoreCase: true, out var mode))
        {
            throw new ValidationException("dependsOn", $"Mode '{dependency.Mode}' must be blocking, soft or advisory.");
        }

        return mode;
    }

    private static void Apply(string field, Action action)
    {
        try
        {
            action();
        }
        catch (DomainException ex)
        {
            throw new ValidationException(field, ex.Message);
        }
    }
}
