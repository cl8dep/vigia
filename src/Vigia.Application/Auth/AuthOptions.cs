namespace Vigia.Application.Auth;

/// <summary>
/// Authentication settings, bound from the <c>Auth</c> config section.
/// </summary>
public sealed class AuthOptions
{
    /// <summary>Config section name.</summary>
    public const string Section = "Auth";

    /// <summary>
    /// Allow anyone to sign up. Off by default: only the first user can sign up, to bootstrap the instance.
    /// </summary>
    public bool OpenSignUp { get; set; }
}
