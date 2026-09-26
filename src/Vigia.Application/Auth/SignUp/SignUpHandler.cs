using Mediator;
using Microsoft.Extensions.Options;
using Vigia.Application.Common.Exceptions;

namespace Vigia.Application.Auth.SignUp;

/// <summary>
/// Handles <see cref="SignUpCommand"/>. Only the first user may sign up unless <see cref="AuthOptions.OpenSignUp"/> is on.
/// </summary>
public sealed class SignUpHandler(IIdentityService identity, IOptions<AuthOptions> options) : ICommandHandler<SignUpCommand, UserDto>
{
    /// <inheritdoc />
    public async ValueTask<UserDto> Handle(SignUpCommand command, CancellationToken ct)
    {
        if (!options.Value.OpenSignUp && await identity.AnyUsersAsync(ct))
        {
            throw new ForbiddenException("Sign-up is closed. Ask an administrator for an invitation.");
        }

        return await identity.CreateUserAsync(command.Email, command.Password, ct);
    }
}
