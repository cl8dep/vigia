using Mediator;

namespace Vigia.Application.Auth.SignIn;

/// <summary>
/// Handles <see cref="SignInCommand"/>.
/// </summary>
public sealed class SignInHandler(IIdentityService identity) : ICommandHandler<SignInCommand, AuthTokens>
{
    /// <inheritdoc />
    public async ValueTask<AuthTokens> Handle(SignInCommand command, CancellationToken ct)
    {
        return await identity.SignInAsync(command.Email, command.Password, ct);
    }
}
