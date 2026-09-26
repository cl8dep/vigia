using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vigia.Application.Auth;
using Vigia.Application.Auth.Refresh;
using Vigia.Application.Auth.SignIn;
using Vigia.Application.Auth.SignUp;

namespace Vigia.Api.Controllers;

/// <summary>
/// Email and password sign-up and sign-in.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates an account. Only the first user can sign up unless <c>Auth:OpenSignUp</c> is on.
    /// </summary>
    [HttpPost("sign-up")]
    public async Task<ActionResult<UserDto>> SignUp(SignUpCommand command, CancellationToken ct)
    {
        var user = await mediator.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Signs in and returns a bearer token.</summary>
    [HttpPost("sign-in")]
    public async Task<ActionResult<AuthTokens>> SignIn(SignInCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }

    /// <summary>Exchanges a refresh token for new tokens.</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthTokens>> Refresh(RefreshCommand command, CancellationToken ct)
    {
        return Ok(await mediator.Send(command, ct));
    }
}
