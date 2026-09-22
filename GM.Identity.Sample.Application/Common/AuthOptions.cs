namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Authentication tuning bound from the <c>Auth</c> configuration section: access/refresh token lifetimes
/// and the failed-login lockout policy.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Access-token lifetime in minutes (short-lived; refreshed via the refresh token).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh-token / session lifetime in days (absolute — rotation does not extend it).</summary>
    public int RefreshTokenDays { get; set; } = 30;

    /// <summary>Consecutive failed logins that trigger a lockout.</summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>How long an account stays locked after too many failed logins, in minutes.</summary>
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Maximum concurrent sessions a user may hold; the oldest are revoked past this. 0 = unlimited.</summary>
    public int MaxConcurrentSessionsPerUser { get; set; }

    /// <summary>Days before a password must be changed; login is refused past this. 0 = never expires.</summary>
    public int PasswordExpiryDays { get; set; }

    /// <summary>
    /// When <c>true</c>, login is refused while the user still owes acceptance of a mandatory consent document
    /// at its current version (the outstanding documents are available via GET users/me/Consents/Pending).
    /// Off by default — enforcement is a deployment policy, like <see cref="PasswordExpiryDays"/>.
    /// </summary>
    public bool EnforceConsent { get; set; }

    /// <summary>
    /// Single sign-on session lifetime in minutes — how long one interactive login lets the browser obtain
    /// authorization codes for other clients silently before it must re-authenticate. Defaults to 8 hours.
    /// </summary>
    public int SsoSessionMinutes { get; set; } = 480;
}
