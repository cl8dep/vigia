using Microsoft.AspNetCore.Diagnostics;
using Vigia.Application.Common.Exceptions;

namespace Vigia.Api.Errors;

/// <summary>
/// Maps application exceptions to RFC 9457 problem responses.
/// </summary>
public sealed class ProblemExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        IResult? result = exception switch
        {
            ValidationException ex => Results.ValidationProblem(ex.Errors.ToDictionary()),
            NotFoundException ex => Results.Problem(ex.Message, statusCode: StatusCodes.Status404NotFound),
            ConflictException ex => Results.Problem(ex.Message, statusCode: StatusCodes.Status409Conflict),
            UnauthorizedException ex => Results.Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized),
            ForbiddenException ex => Results.Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden),
            _ => null,
        };

        if (result is null)
        {
            return false;
        }

        await result.ExecuteAsync(http);
        return true;
    }
}
