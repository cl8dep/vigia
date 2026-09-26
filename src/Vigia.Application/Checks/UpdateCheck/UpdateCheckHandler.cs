using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;
using Vigia.Domain.Common;

namespace Vigia.Application.Checks.UpdateCheck;

/// <summary>
/// Handles <see cref="UpdateCheckCommand"/>.
/// </summary>
public sealed class UpdateCheckHandler(IAppDbContext db, IPluginRegistry registry) : ICommandHandler<UpdateCheckCommand, CheckDto>
{
    /// <inheritdoc />
    public async ValueTask<CheckDto> Handle(UpdateCheckCommand command, CancellationToken ct)
    {
        var check = await db.Checks.SingleOrDefaultAsync(c => c.Slug == command.Slug, ct)
            ?? throw new NotFoundException("check", command.Slug);

        if (command.Plugin is not null && command.Plugin != check.Plugin)
        {
            throw new ValidationException("plugin", "The plugin of a check cannot change. Delete the check and create a new one.");
        }

        if (!registry.TryGetCheck(check.Plugin, out var plugin))
        {
            throw new ValidationException("plugin", $"Plugin '{check.Plugin}' is not installed.");
        }

        var interval = plugin.Check.Manifest.DefaultInterval;
        if (command.Interval is not null && !Duration.TryParse(command.Interval, out interval))
        {
            throw new ValidationException("interval", $"'{command.Interval}' is not a duration. Use values like 30s, 5m.");
        }

        var config = SecretFields.KeepMissing(command.Config, check.ConfigJson, plugin.Schema);
        var bound = ConfigBinder.Bind(plugin, config, "config");

        try
        {
            check.Rename(command.Name ?? command.Slug);
            check.Reconfigure(plugin.Version, bound.Normalized.GetRawText(), interval);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(string.Empty, ex.Message);
        }

        check.SetLabels(command.Labels ?? new Dictionary<string, string>());
        if (command.Enabled ?? true)
        {
            check.Enable();
        }
        else
        {
            check.Disable();
        }

        await db.SaveChangesAsync(ct);
        return CheckDto.From(check, plugin);
    }
}
