namespace Vigia.Infrastructure.Auth;

/// <summary>
/// Names for worker authentication: a separate scheme and policy so worker credentials and user tokens never mix.
/// </summary>
public static class WorkerAuthentication
{
    /// <summary>Authentication scheme; also the word in <c>Authorization: Worker &lt;credential&gt;</c>.</summary>
    public const string Scheme = "Worker";

    /// <summary>Authorization policy for worker-only endpoints.</summary>
    public const string Policy = "Worker";

    /// <summary>Claim carrying the worker slug.</summary>
    public const string SlugClaim = "vigia:worker";
}
