using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Json;
using Vigia.Domain.Services;

namespace Vigia.Application.Services;

/// <summary>
/// Builds <see cref="ServiceDto"/>s, resolving check selectors and rule coverage in bulk.
/// </summary>
public static class ServiceViews
{
    /// <summary>Maps services, with their matched and uncovered checks.</summary>
    public static async Task<IReadOnlyList<ServiceDto>> BuildAsync(IAppDbContext db, IReadOnlyList<Service> services, CancellationToken ct)
    {
        var slugs = await db.Services.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Slug, ct);
        var checks = await db.Checks.AsNoTracking().OrderBy(c => c.Slug).ToListAsync(ct);
        var rules = await db.Rules.AsNoTracking().Where(r => r.Enabled).ToListAsync(ct);

        return services.Select(service =>
        {
            var matched = service.Checks is null ? [] : checks.Where(c => service.Checks.Matches(c.Tags)).ToList();
            var uncovered = matched.Where(c => !rules.Any(r => r.Targets(c.Id, c.Tags))).Select(c => c.Slug).ToList();
            var dependsOn = service.Dependencies
                .Select(d => new DependencyDto(slugs.GetValueOrDefault(d.DependsOnId, d.DependsOnId.ToString()), JsonNamingPolicy.CamelCase.ConvertName(d.Mode.ToString())))
                .OrderBy(d => d.Service)
                .ToList();

            return new ServiceDto(
                service.Slug,
                service.Name,
                service.Tags,
                service.Checks is null ? null : TagSelectorJson.Write(service.Checks),
                service.PartitionBy,
                dependsOn,
                [.. matched.Select(c => c.Slug)],
                uncovered,
                JsonNamingPolicy.CamelCase.ConvertName(service.ManagedBy.ToString()));
        }).ToList();
    }
}
