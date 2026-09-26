using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Security;
using Vigia.Application.Plugins;

namespace Vigia.Application.Workers.Assignments;

/// <summary>
/// Handles <see cref="GetAssignmentsQuery"/>.
/// </summary>
public sealed class GetAssignmentsHandler(IAppDbContext db, IPluginRegistry registry) : IQueryHandler<GetAssignmentsQuery, AssignmentsDto>
{
    /// <inheritdoc />
    public async ValueTask<AssignmentsDto> Handle(GetAssignmentsQuery query, CancellationToken ct)
    {
        var worker = await db.Workers.AsNoTracking().SingleOrDefaultAsync(w => w.Id == query.WorkerId, ct)
            ?? throw new UnauthorizedException("Unknown worker.");

        var checks = await WorkerAssignments.ForAsync(worker, db, registry, ct);
        var assignments = checks
            .Select(c => new AssignmentDto(
                c.Id,
                c.Slug,
                c.Plugin,
                c.PluginVersion,
                JsonDocument.Parse(c.ConfigJson).RootElement.Clone(),
                Duration.Format(c.Interval),
                c.UpdatedAt))
            .ToList();

        // Worker tags matter too: retagging a worker changes its set without touching any check.
        var revisionSource = string.Join('|', assignments.Select(a => $"{a.CheckId}:{a.Version:O}")) + "|" + worker.UpdatedAt.ToString("O");
        return new AssignmentsDto(SecretTokens.Hash(revisionSource)[..16], assignments);
    }
}
