using Mediator;

namespace Vigia.Application.Auth.Refresh;

/// <summary>
/// Handles <see cref="RefreshCommand"/>.
/// </summary>
public sealed class RefreshHandler(IIdentityService identity) : ICommandHandler<RefreshCommand, AuthTokens>
{
    /// <inheritdoc />
    public async ValueTask<AuthTokens> Handle(RefreshCommand command, CancellationToken ct)
    {
        return await identity.RefreshAsync(command.RefreshToken, ct);
    }
}
