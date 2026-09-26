using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Workers;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;

namespace Vigia.Application.Checks.ProbeCheck;

/// <summary>
/// Handles <see cref="ProbeCheckCommand"/> through the same <see cref="IProbeExecutor"/> the workers use.
/// </summary>
public sealed class ProbeCheckHandler(IAppDbContext db, IProbeExecutor executor) : ICommandHandler<ProbeCheckCommand, ProbeResultDto>
{
    /// <inheritdoc />
    public async ValueTask<ProbeResultDto> Handle(ProbeCheckCommand command, CancellationToken ct)
    {
        var check = await db.Checks.AsNoTracking().SingleOrDefaultAsync(c => c.Slug == command.Slug, ct)
            ?? throw new NotFoundException("check", command.Slug);

        var record = await executor.ExecuteAsync(CheckAssignment.From(check), ct);

        return new ProbeResultDto(
            JsonNamingPolicy.CamelCase.ConvertName(record.Outcome.ToString()),
            record.Measurements,
            record.Message,
            record.DurationMs);
    }
}
