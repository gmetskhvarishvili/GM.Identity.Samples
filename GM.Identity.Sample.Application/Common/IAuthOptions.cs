namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Authentication tuning consumed by the Application layer. The concrete options class that binds the <c>Auth</c>
/// configuration section lives in the Infrastructure layer and implements this interface; Infrastructure registers
/// it so Application handlers can depend on the settings without referencing Infrastructure.
/// </summary>
public interface IAuthOptions
{
    /// <summary>Access-token lifetime in minutes.</summary>
    int AccessTokenMinutes { get; }

    /// <summary>Refresh-token / session lifetime in days.</summary>
    int RefreshTokenDays { get; }

    /// <summary>Consecutive failed logins that trigger a lockout.</summary>
    int MaxFailedAccessAttempts { get; }

    /// <summary>How long an account stays locked after too many failed logins, in minutes.</summary>
    int LockoutMinutes { get; }

    /// <summary>Maximum concurrent sessions a user may hold; the oldest are revoked past this. 0 = unlimited.</summary>
    int MaxConcurrentSessionsPerUser { get; }

    /// <summary>Single sign-on session lifetime in minutes.</summary>
    int SsoSessionMinutes { get; }
}
