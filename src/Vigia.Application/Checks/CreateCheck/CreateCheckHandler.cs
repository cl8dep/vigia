using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Plugins;
using Vigia.Application.Webhooks;
using Vigia.Domain.Checks;
using Vigia.Domain.Common;

namespace Vigia.Application.Checks.CreateCheck;

/// <summary>
/// Handles <see cref="CreateCheckCommand"/>.
/// </summary>
public sealed class CreateCheckHandler(IAppDbContext db, IPluginRegistry registry) : ICommandHandler<CreateCheckCommand, CheckDto>
{
    /// <inheritdoc />
    public async ValueTask<CheckDto> Handle(CreateCheckCommand command, CancellationToken ct)
    {
        if (!registry.TryGetCheck(command.Plugin, out var plugin))
        {
            throw new ValidationException("plugin", $"Plugin '{command.Plugin}' is not installed.");
        }

        var interval = plugin.DefaultInterval;
        if (command.Interval is not null && !Duration.TryParse(command.Interval, out interval))
        {
            throw new ValidationException("interval", $"'{command.Interval}' is not a duration. Use values like 30s, 5m.");
        }

        var bound = ConfigBinder.Bind(plugin, command.Config, "config");

        if (await db.Checks.AnyAsync(c => c.Slug == command.Slug, ct))
        {
            throw new ConflictException($"A check with slug '{command.Slug}' already exists.");
        }

        Check check;
        try
        {
            check = new Check(command.Slug, command.Name ?? command.Slug, plugin.Id, plugin.Version, bound.Normalized.GetRawText(), interval, ManagedBy.Ui);
        }
        catch (DomainException ex)
        {
            throw new ValidationException(string.Empty, ex.Message);
        }

        try
        {
            check.SetTags(command.Tags ?? new Dictionary<string, string?>());
        }
        catch (DomainException ex)
        {
            throw new ValidationException("tags", ex.Message);
        }

        string? webhookToken = null;
        if (plugin.Webhooks.Count > 0)
        {
            (webhookToken, var hash) = WebhookTokens.Create();
            check.SetWebhookTokenHash(hash);
        }

        db.Checks.Add(check);
        await db.SaveChangesAsync(ct);

        return CheckDto.From(check, plugin, webhookToken);
    }
}
