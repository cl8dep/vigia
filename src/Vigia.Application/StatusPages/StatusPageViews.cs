using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Domain.Common;
using Vigia.Domain.StatusPages;

namespace Vigia.Application.StatusPages;

/// <summary>
/// Applies specs to status pages and maps them back.
/// </summary>
public static class StatusPageViews
{
    /// <summary>Applies <paramref name="spec"/>.</summary>
    /// <exception cref="ValidationException">A component names an unknown service or is invalid.</exception>
    public static async Task ApplyAsync(StatusPage page, string slug, StatusPageSpec spec, IAppDbContext db, CancellationToken ct)
    {
        var requested = spec.Components ?? [];
        var slugs = requested.Select(c => c.Service).Distinct().ToList();
        var services = await db.Services.AsNoTracking().Where(s => slugs.Contains(s.Slug)).ToDictionaryAsync(s => s.Slug, s => s.Id, ct);
        var missing = slugs.Where(s => !services.ContainsKey(s)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException("components", $"Unknown service(s): {string.Join(", ", missing)}.");
        }

        try
        {
            page.Rename(spec.Title ?? slug);
            page.Describe(spec.Description);
            page.SetTags(spec.Tags ?? new Dictionary<string, string?>());
            page.Show(requested.Select(c => (services[c.Service], c.Name, c.Group)).ToList());
        }
        catch (DomainException ex)
        {
            throw new ValidationException("components", ex.Message);
        }
    }

    /// <summary>Maps a page for admins.</summary>
    public static async Task<StatusPageDto> ToDtoAsync(StatusPage page, IAppDbContext db, CancellationToken ct)
    {
        var ids = page.Components.Select(c => c.ServiceId).ToList();
        var slugs = await db.Services.AsNoTracking().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Slug, ct);
        return new StatusPageDto(
            page.Slug,
            page.Name,
            page.Description,
            page.Tags,
            page.Components.OrderBy(c => c.Order).Select(c => new ComponentSpec(slugs.GetValueOrDefault(c.ServiceId, "?"), c.Name, c.Group)).ToList());
    }
}
