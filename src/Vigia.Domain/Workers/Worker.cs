using Vigia.Domain.Common;
using Vigia.Domain.Tags;

namespace Vigia.Domain.Workers;

/// <summary>
/// A process that runs checks: the built-in one inside the control plane, or a remote one in another region or network.
/// Remote workers enroll once with a one-time token and then authenticate with their own credential.
/// </summary>
public sealed class Worker : Entity
{
    /// <summary>How long an enrollment token stays valid.</summary>
    public static readonly TimeSpan EnrollmentTokenLifetime = TimeSpan.FromHours(24);

    private Worker()
    {
    }

    /// <inheritdoc />
    protected override TaggedEntity TagKind
    {
        get { return TaggedEntity.Worker; }
    }

    /// <summary>Creates a worker.</summary>
    /// <exception cref="DomainException">The slug, name or region is invalid.</exception>
    public Worker(string slug, string name, string? region, bool builtIn, ManagedBy managedBy)
        : base(slug, name, managedBy)
    {
        BuiltIn = builtIn;
        if (builtIn)
        {
            SetSystemTag(SystemTags.Builtin, null);
        }

        MoveTo(region);
    }

    /// <summary>Region the worker runs in, mirrored as the <c>vigia:region</c> system tag.</summary>
    public string? Region { get; private set; }

    /// <summary>The worker inside the control plane. It has no credential and cannot be deleted.</summary>
    public bool BuiltIn { get; private set; }

    /// <summary>SHA-256 of the worker's credential, or null when not enrolled or revoked.</summary>
    public string? CredentialHash { get; private set; }

    /// <summary>SHA-256 of a pending enrollment token, or null.</summary>
    public string? EnrollmentTokenHash { get; private set; }

    /// <summary>When the pending enrollment token stops being valid.</summary>
    public DateTimeOffset? EnrollmentExpiresAt { get; private set; }

    /// <summary>Last time the worker reported in.</summary>
    public DateTimeOffset? LastSeenAt { get; private set; }

    /// <summary>Worker software version from its last report.</summary>
    public string? Version { get; private set; }

    /// <summary>Whether the worker holds a valid credential.</summary>
    public bool Enrolled
    {
        get { return CredentialHash is not null; }
    }

    /// <summary>Sets the region and the matching system tag.</summary>
    /// <exception cref="DomainException">The region is not a valid tag value.</exception>
    public void MoveTo(string? region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            Region = null;
            RemoveSystemTag(SystemTags.Region);
            return;
        }

        TagRules.ValidateValue(SystemTags.Region, region);
        Region = region.Trim();
        SetSystemTag(SystemTags.Region, Region);
    }

    /// <summary>Stores a new enrollment token hash. Any earlier pending token stops working; the current credential does not.</summary>
    /// <exception cref="DomainException">The worker is built in.</exception>
    public void IssueEnrollmentToken(string hash, DateTimeOffset now)
    {
        EnsureRemote();
        EnrollmentTokenHash = hash;
        EnrollmentExpiresAt = now + EnrollmentTokenLifetime;
    }

    /// <summary>Whether a pending enrollment token is still usable.</summary>
    public bool CanEnroll(DateTimeOffset now)
    {
        return EnrollmentTokenHash is not null && EnrollmentExpiresAt > now;
    }

    /// <summary>Consumes the enrollment token and stores the new credential hash, replacing any previous credential.</summary>
    /// <exception cref="DomainException">No valid enrollment token is pending.</exception>
    public void Enroll(string credentialHash, DateTimeOffset now)
    {
        if (!CanEnroll(now))
        {
            throw new DomainException("No valid enrollment token.");
        }

        CredentialHash = credentialHash;
        EnrollmentTokenHash = null;
        EnrollmentExpiresAt = null;
    }

    /// <summary>Invalidates the credential. The worker must enroll again with a new token.</summary>
    /// <exception cref="DomainException">The worker is built in.</exception>
    public void Revoke()
    {
        EnsureRemote();
        CredentialHash = null;
    }

    /// <summary>Records a report from the worker.</summary>
    public void Seen(DateTimeOffset at, string? version)
    {
        LastSeenAt = at;
        Version = version ?? Version;
    }

    private void EnsureRemote()
    {
        if (BuiltIn)
        {
            throw new DomainException("The built-in worker runs inside the control plane and has no credential.");
        }
    }
}
