using System.Collections.Generic;

namespace GM.Identity.Sample.Infrastructure.Options;

/// <summary>How an external provider yields the user's email once the code is exchanged.</summary>
public enum ProviderKind
{
    /// <summary>OpenID Connect: the token response carries an <c>id_token</c> whose email claim is read.</summary>
    Oidc,

    /// <summary>Plain OAuth 2.0: an access token is used to call a userinfo endpoint that returns the email.</summary>
    OAuth2,
}

/// <summary>How the access token is presented to an OAuth 2.0 userinfo endpoint.</summary>
public enum UserInfoTokenPlacement
{
    /// <summary><c>Authorization: Bearer &lt;token&gt;</c> (e.g. GitHub).</summary>
    Bearer,

    /// <summary><c>?access_token=&lt;token&gt;</c> query parameter (e.g. Facebook Graph).</summary>
    Query,
}

/// <summary>How the provider's client secret is obtained.</summary>
public enum SecretKind
{
    /// <summary>A static secret string, resolved from the secrets store.</summary>
    Static,

    /// <summary>
    /// A short-lived ES256 JWT generated per request from an EC private key — Sign in with Apple's
    /// client-secret scheme (the stored secret is the .p8 private key, not the client_secret itself).
    /// </summary>
    AppleJwt,
}

/// <summary>
/// Configuration for one external identity provider. Google/Microsoft/LinkedIn/Apple and any enterprise
/// OpenID Connect IdP are <see cref="ProviderKind.Oidc"/> (read the id_token); Facebook/GitHub are
/// <see cref="ProviderKind.OAuth2"/> (call a userinfo endpoint). Adding a spec-compliant OIDC provider is
/// configuration-only.
/// </summary>
public class OAuthProviderOptions
{
    public string ClientId { get; set; } = default!;

    /// <summary>The static client secret (ignored when <see cref="SecretKind"/> is <see cref="SecretKind.AppleJwt"/>).</summary>
    public string ClientSecret { get; set; } = default!;

    public string AuthorizationEndpoint { get; set; } = default!;
    public string TokenEndpoint { get; set; } = default!;
    public string Scope { get; set; } = default!;

    /// <summary>Which field of the token response holds the credential to read — <c>id_token</c> (OIDC) or <c>access_token</c> (OAuth2).</summary>
    public string TokenName { get; set; } = "id_token";

    /// <summary>OAuth2 userinfo endpoint (only for <see cref="ProviderKind.OAuth2"/>).</summary>
    public string? UserDetailsEndpoint { get; set; }

    /// <summary>How the email is extracted after the code exchange.</summary>
    public ProviderKind Kind { get; set; } = ProviderKind.Oidc;

    /// <summary>OIDC: the id_token claim that carries the email.</summary>
    public string EmailClaim { get; set; } = "email";

    /// <summary>OAuth2: the userinfo JSON field that carries the email.</summary>
    public string EmailField { get; set; } = "email";

    /// <summary>OAuth2: how the access token is sent to the userinfo endpoint.</summary>
    public UserInfoTokenPlacement UserInfoTokenPlacement { get; set; } = UserInfoTokenPlacement.Bearer;

    /// <summary>How the client secret is obtained for the token exchange.</summary>
    public SecretKind SecretKind { get; set; } = SecretKind.Static;

    /// <summary>Extra parameters appended to the authorization request (e.g. Google's <c>access_type</c>/<c>prompt</c>, Apple's <c>response_mode</c>).</summary>
    public Dictionary<string, string> AdditionalAuthorizationParameters { get; set; } = new();

    /// <summary>Sign in with Apple: the Apple Developer Team ID (JWT <c>iss</c>). Required when <see cref="SecretKind"/> is <see cref="SecretKind.AppleJwt"/>.</summary>
    public string? TeamId { get; set; }

    /// <summary>Sign in with Apple: the key identifier of the .p8 private key (JWT header <c>kid</c>).</summary>
    public string? KeyId { get; set; }
}

public class OAuthOptions : Dictionary<string, OAuthProviderOptions>;
