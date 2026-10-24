namespace GM.Identity.Sample.Persistence.Configuration;

/// <summary>
/// The sample's names for GM.Identity's tables: the same snake_case schemas and tables GM.Identity uses by default
/// (<c>GMIdentityTableNames</c>), without the <c>gm_</c> prefix. Each sample configuration overrides its base's
/// <c>Schema</c> and <c>Table</c> with these — showing how an application maps GM.Identity into its own naming.
/// </summary>
public static class IdentityTableNames
{
    public static class Schemas
    {
        public const string Identity = "identity";
        public const string AccessControl = "access_control";
        public const string Authorization = "authorization";
        public const string Compliance = "compliance";
    }

    // Identity
    public const string Users = "users";
    public const string UserPersonalInfo = "user_personal_info";
    public const string UserPasskeys = "user_passkeys";
    public const string PasskeyChallenges = "passkey_challenges";
    public const string TwoFactorAuthTypes = "two_factor_auth_types";
    public const string UserTwoFactorAuthTypes = "user_two_factor_auth_types";
    public const string Clients = "clients";
    public const string ClientRedirectUris = "client_redirect_uris";

    // Access control
    public const string Roles = "roles";
    public const string Permissions = "permissions";
    public const string RolePermissions = "role_permissions";
    public const string UserRoles = "user_roles";
    public const string Scopes = "scopes";
    public const string Operations = "operations";
    public const string ScopeOperations = "scope_operations";
    public const string ClientScopes = "client_scopes";

    // Authorization
    public const string UserSessions = "user_sessions";
    public const string ClientSessions = "client_sessions";
    public const string SsoSessions = "sso_sessions";
    public const string AuthorizationCodes = "authorization_codes";
    public const string DeviceCodes = "device_codes";
    public const string UserClientConsents = "user_client_consents";

    // Compliance
    public const string ConsentDocuments = "consent_documents";
    public const string UserConsents = "user_consents";
}
