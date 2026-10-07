# Configuration reference

Every setting the sample reads, grouped by host. Values shown are the defaults that ship in `appsettings.json` / the `Options` classes. Override any of them per environment via `appsettings.{Environment}.json`, environment variables (`Section__Key`), or a secret store — **never commit real secrets** (the committed values are local-dev placeholders).

- [Identity API](#identity-api)
- [Gateways (public & admin)](#gateways)
- [OTP service](#otp-service)
- [Notifications service](#notifications-service)
- [Overriding settings](#overriding-settings)

---

## Identity API

`GM.Identity.Sample.API` — bound in `Infrastructure` via the `Options` classes.

### Connections

| Key | Default | Notes |
|---|---|---|
| `ConnectionStrings:ApplicationDatabase` | `Host=localhost;Port=5432;Database=GMIdentitySample;…` | PostgreSQL (Npgsql). |
| `ConnectionStrings:Redis` | `localhost:6379` | Session / RBAC / scope projections, locks. |

### `Auth` (`AuthOptions`)

| Key | Default | Meaning |
|---|---|---|
| `AccessTokenMinutes` | `15` | Access-token lifetime. |
| `RefreshTokenDays` | `30` | Refresh-token lifetime. |
| `MaxFailedAccessAttempts` | `5` | Failed password checks before lockout. |
| `LockoutMinutes` | `15` | Lockout duration once tripped. |
| `MaxConcurrentSessionsPerUser` | `0` | `0` = unlimited. |
| `SsoSessionMinutes` | `480` | SSO browser-session lifetime (8 h). |

### `PasswordPolicy` (`PasswordPolicyOptions`)

| Key | Default |
|---|---|
| `MinimumLength` | `8` |
| `RequireUppercase` | `true` |
| `RequireLowercase` | `true` |
| `RequireDigit` | `true` |
| `RequireNonAlphanumeric` | `false` |
| `CheckForBreaches` | `false` |

### `Retention` (`RetentionOptions`)

| Key | Default | Meaning |
|---|---|---|
| `AuditRetentionDays` | `365` | How long the domain-event / audit log is kept. |

### `Seed`

Development seeding only — remove or disable in production.

| Key | Default | Meaning |
|---|---|---|
| `AdminPassword` | `Admin123!` | Password for the seeded admin. |
| `ClientId` | `11111111-…-111111111111` | Seeded OAuth client id. |
| `ClientSecret` | `secret` | Seeded client secret. |
| `ConsentDocuments` | `true` | Seed sample consent documents. |

### `OAuth` — external / social providers (`OAuthOptions` per provider)

One block per provider (`Google`, `Microsoft`, `LinkedIn`, `Apple`, `Facebook`). All ship with `YOUR_…` placeholders.

| Key | Meaning |
|---|---|
| `Kind` | `Oidc` or `OAuth2`. |
| `ClientId` / `ClientSecret` | Provider app credentials. |
| `AuthorizationEndpoint` / `TokenEndpoint` | Provider endpoints. |
| `UserDetailsEndpoint` | OAuth2 only (e.g. Facebook `/me`). |
| `Scope` | Requested scopes (e.g. `openid email profile`). |
| `TokenName` | Which token carries identity (`id_token` / `access_token`). |
| `EmailClaim` / `EmailField` | Where to read the email (claim for OIDC, field for OAuth2). |
| `SecretKind` | `Static` or `AppleJwt` (Apple signs a client-secret JWT with `TeamId` + `KeyId`). |
| `AdditionalAuthorizationParameters` | Extra query params (e.g. `access_type=offline`). |

### `ApiServices:OTPAPIService` — HTTP client to the OTP service

Resilience is configured declaratively (`GM.HttpClient`).

| Key | Default | Meaning |
|---|---|---|
| `BaseUrl` | `http://localhost:5250/api/v1.0/` | OTP service base URL. |
| `Headers` | `X-Client-Id`, `X-Environment` | Sent on every call. |
| `RetryPolicy:RetryCount` | `3` | Transient-fault retries. |
| `CircuitBreakerPolicy` | `FailureThreshold=5`, `BreakDuration=00:00:30` | Trip + cool-down. |
| `TimeoutPolicy:Timeout` | `00:00:10` | Per-call timeout. |
| `FallbackPolicy` | `UseFallback=true` | Response when the call fails open. |
| `HedgingPolicy` | `HedgingAttempts=2`, `HedgingDelay=100ms` | Parallel hedged requests. |
| `Authorization` | `Basic` (dev creds) | Credentials for the OTP API. |
| `Caching` | `EnableCaching=true`, `CacheDuration=00:05:00` | Response caching. |

### Other

| Key | Meaning |
|---|---|
| `Service:Name` | Logical service name (telemetry / health). |
| `AllowedHosts` | Host filtering (`*` in dev). |
| `SwaggerDocOptions` | OpenAPI document metadata. |
| `Serilog` | Structured logging sinks/levels. |
| OpenTelemetry (code-wired) | OTLP exporter endpoint via `OTEL_EXPORTER_OTLP_ENDPOINT`. |

---

## Gateways

`GM.Identity.Sample.Gateway.API` (public) and `GM.Identity.Sample.Admin.Gateway.API` (admin). Both use YARP + `GM.Gateway` and share the same shape.

| Key | Meaning |
|---|---|
| `ConnectionStrings:Redis` | Backs rate limiting, caching, and distributed locks. |
| `ReverseProxy:Routes` | One entry per route: `Match` (`Path`, `Methods`), `ClusterId`, optional `AuthorizationPolicy`, and `Metadata` (`RateLimitPolicy`, `RateLimitPartition`). |
| `ReverseProxy:Clusters` | Backend destinations, e.g. `identity-api` → `https://localhost:7122/`. |
| `RateLimiting:Policies` | Named policies referenced by route metadata. |

### Rate-limit policies (defaults)

| Policy | Algorithm | Limit | Applied to |
|---|---|---|---|
| `api` | FixedWindow | 60 / min | general API routes (`/me`, registration, admin resources) |
| `auth` | FixedWindow | 10 / min | `/connect/*` (login, token, code endpoints) |

`RateLimitPartition` (e.g. `Ip,Endpoint`) sets what the counter is keyed on.

**Route differences:** the public gateway leaves `/connect/*` and registration anonymous and marks `/me` / `/me/sessions` with `AuthorizationPolicy: default`; the admin gateway sets `AuthorizationPolicy: default` on **all** RBAC/admin routes. (In dev the cluster allows any server cert via `HttpClient:DangerousAcceptAnyServerCertificate` — remove in production.)

---

## OTP service

`GM.OTP.Sample.API`.

| Key | Default | Meaning |
|---|---|---|
| `ConnectionStrings:ApplicationDatabase` | `…Database=GMOTPSample…` | OTP service PostgreSQL. |
| `OtpOptions:CodeLength` | `6` | Delivered-code length. |
| `OtpOptions:ExpirationMinutes` | `2` | Code lifetime. |
| `OtpOptions:MaxAttempts` | `5` | Verification attempts before invalidation. |
| `OtpOptions:MessageTemplate` | `Your OTP code is: {code}` | Delivery message template. |

**TOTP** (authenticator app) is served in the Identity API by `AddGMTotp()` from GM.OTP; its `TotpOptions` default to 6 digits, a 30-second period, a ±1-step verification window, and a 20-byte secret. Pass a configure delegate to change them.

---

## Notifications service

`GM.Notifications.Sample.API` enqueues; each channel worker (`EmailWorker`, `SmsWorker`, `PushWorker`, `SlackWorker`, `WhatsAppWorker`) delivers.

| Key | Meaning |
|---|---|
| `ConnectionStrings:ApplicationDatabase` | Notifications PostgreSQL (`GMNotificationsSample`). |
| Per-worker provider settings | Each channel worker's `appsettings.json` holds its provider config (SMTP host/port/credentials for email, the SMS/push/Slack/WhatsApp provider keys) plus its poll interval and batch size. |

> These provider credentials are the clearest candidates for a secret store rather than `appsettings` — see [security.md](security.md).

---

## Overriding settings

Precedence (later wins): `appsettings.json` → `appsettings.{Environment}.json` → environment variables → command line.

- **Nested keys** use `__` (double underscore) as the separator in environment variables:
  ```bash
  Auth__AccessTokenMinutes=30
  ConnectionStrings__Redis=redis:6379
  RateLimiting__Policies__auth__PermitLimit=5
  ```
- **Secrets in development**: use `dotnet user-secrets` for the API/gateway projects instead of editing `appsettings.json`.
- **Secrets in production**: load connection strings, provider credentials, and signing material from a secret manager (`GM.Secrets` is wired for this), not from files.

## Related

- [security.md](security.md) · [architecture-decisions.md](architecture-decisions.md) · [tenancy.md](tenancy.md) · [events.md](events.md) · [extending.md](extending.md)
