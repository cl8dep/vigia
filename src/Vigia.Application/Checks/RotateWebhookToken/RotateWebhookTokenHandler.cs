using Mediator;
using Microsoft.EntityFrameworkCore;
using Vigia.Application.Common.Exceptions;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Common.Security;
using Vigia.Application.Plugins;

namespace Vigia.Application.Checks.RotateWebhookToken;

/// <summary>
/// Handles <see cref="RotateWebhookTokenCommand"/>.
/// </summary>
public sealed class RotateWebhookTokenHandler(IAppDbContext db, IPluginRegistry registry) : ICommandHandler<RotateWebhookTokenCommand, WebhookTokenDto>
{
    /// <inheritdoc />
    public async ValueTask<WebhookTokenDto> Handle(RotateWebhookTokenCommand command, CancellationToken ct)
    {
        var check = await db.Checks.SingleOrDefaultAsync(c => c.Slug == command.Slug, ct)
            ?? throw new NotFoundException("check", command.Slug);

        if (!registry.TryGetCheck(check.Plugin, out var plugin) || plugin.Webhooks.Count == 0)
        {
            throw new ValidationException("plugin", $"Plugin '{check.Plugin}' declares no webhooks.");
        }

        var (token, hash) = SecretTokens.Create();
        check.SetWebhookTokenHash(hash);
        await db.SaveChangesAsync(ct);
        return new WebhookTokenDto(token);
    }
}
