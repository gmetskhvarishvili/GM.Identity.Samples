# Security model & threat notes

How the GM.Identity sample protects credentials, tokens, and data — and, just as important, **where it is deliberately sample-grade** and must be hardened before production use.

> This document describes the sample. It is a reference implementation for the GM.\* libraries, not a certified identity provider. The "Production checklist" at the end lists what is intentionally out of scope.

## Trust boundaries

```
Internet ──▶ Gateway (YARP) ──▶ Identity API ──▶ PostgreSQL / Redis
                 │                     │
         rate limit, auth,        in-process jobs
         actor forwarding              │
                                  Message bus ──▶ OTP service ──▶ Notifications service
```

1. **Public boundary** — nothing reaches the Identity API directly. The **public gateway** (`GM.Identity.Sample.Gateway.API`) fronts `/connect/*`, registration, and the `/me` self endpoints; the **admin gateway** (`GM.Identity.Sample.Admin.Gateway.API`) fronts the RBAC/admin routes. Both apply per-route rate limiting and forward the authenticated actor's claims to the backend.
2. **Service boundary** — the Identity service never delivers codes or messages itself. It publishes integration events over the bus; the OTP and Notifications services consume them. The only synchronous cross-service call is the Identity API verifying a submitted code against the OTP API over HTTPS.
3. **Data boundary** — only the services reach PostgreSQL and Redis; these are private. Redis holds derived state (sessions, the RBAC/scope projections, rate-limit counters, distributed locks), never the source of truth.

## Credentials & secrets — how each is handled

| Secret | At rest | Verified by |
|---|---|---|
| User password | `PasswordHash` + `PasswordSalt` (`PasswordHasher.Hash`) | `PasswordHasher.Verify` |
| Client secret | `SecretHash` + `SecretSalt`; plaintext returned **once** at register/rotate | `PasswordHasher.Verify` |
| Access / refresh token | Stored only as `TokenGenerator.Hash(...)`; raw value never persisted | hash lookup |
| SSO cookie | Stored as a hash on `SsoSession.TokenHash` | hash lookup |
| Authorization code | Stored as `CodeHash`; bound to client, redirect URI, PKCE challenge, nonce | hash + PKCE |
| Device code | Stored as `DeviceCodeHash`; paired with a short `UserCode` | hash lookup |
| Delivered OTP | Stored hashed + salted on `OtpChallenge` (OTP service), never returned | constant-time compare |
| TOTP secret | `SecretBase32` on the enrolment row | RFC 6238 verify in-process |
| Passkey | **Public key only** (`PublicKeySpki`); the private key never leaves the authenticator | ES256 assertion verify |

Takeaway: no reversible secret is stored. Tokens and codes are opaque random values kept only as hashes, so a database read does not yield anything a caller can replay.

## Tokens & sessions

- **Opaque, not JWT.** Access/refresh tokens are random strings; validity is a lookup against the session store, so revocation is immediate (see ADR-005 in [architecture-decisions.md](architecture-decisions.md)).
- **Fast validation.** A session is projected into Redis keyed by the access-token hash; introspection/userinfo hit the cache first, the database second.
- **Lifetimes** (`AuthOptions`, defaults): access token **15 min** (`AccessTokenMinutes`), refresh token **30 days** (`RefreshTokenDays`).
- **Rotation.** The refresh grant revokes the presented session and issues a fresh pair; the old access-token hash is evicted from the cache.
- **Revocation cascades.** Logout revokes the SSO session and every app session it spawned (via the `SsoSessionId` stamped on each `UserSession`); revoked sessions are evicted from the cache by background jobs. Password change, reset, block, and delete all revoke every session for the user.

## Authentication hardening

- **Lockout.** After `MaxFailedAccessAttempts` (default **5**) failed password checks the account is locked for `LockoutMinutes` (default **15**); the failing user is alerted when a lockout trips.
- **Rate limiting.** Both gateways enforce per-route policies backed by `GM.RateLimiting.Redis`, returning `429` over the limit — this is the first line of defence against credential stuffing and OTP/code brute force.
- **PKCE.** The authorization-code flow binds each code to a `code_challenge`; the token exchange recomputes `PkceHelper.GenerateCodeChallenge(code_verifier)` and rejects a mismatch, so an intercepted code is useless without the verifier.
- **Code brute force.** OTP challenges carry `MaxAttempts` and expiry and are single-use; a wrong code counts a failure and can invalidate the challenge. TOTP verification checks only a small time window.
- **Redirect-URI pinning.** Authorization only redirects to a URI pre-registered for the client (`ClientRedirectUri`), compared verbatim.

## Authorization

- **RBAC projection.** `[HasPermission]` checks the caller's permission against the Redis RBAC projection on the hot path — no database round-trip per request. The projection is kept consistent by domain-event-driven jobs, with a periodic reconciliation job repairing drift (ADR-003/004).
- **Scopes.** Client scopes and scope→operation mappings gate what a client may request; these are projected into a scope cache the same way.
- **Actor forwarding.** The gateway authenticates the opaque token and forwards the principal's claims (user id, session id) as headers, so the backend's audit log records the real actor rather than the proxy.

## Data protection & auditing

- **Soft delete.** Every aggregate is soft-deletable (`IsActive` / `IsDeleted` / `IsHidden`); destructive reads require an explicit `VisibilityScope` opt-in. Nothing is hard-deleted by the normal flows.
- **Audit log.** Domain events are persisted with actor context (user, client, session, tenant, IP, channel, correlation id) and are queryable as an action history.
- **Tenant isolation.** Reads are scoped to the current actor's tenant by a global query filter; see [tenancy.md](tenancy.md).

## Production checklist — what is intentionally out of scope

The sample demonstrates the patterns; a production deployment must additionally:

1. **WebAuthn**: verify **registration attestation** (via a full FIDO2 library), the **RP-ID hash**, and the **user-presence / user-verification** flags. The sample verifies the assertion signature only (see the note on `WebAuthnAssertion`).
2. **Transport**: terminate TLS at/behind the gateway and require HTTPS end to end; set secure/HttpOnly/SameSite on the SSO cookie.
3. **Signing keys**: manage the id_token signing keys (rotation, a real JWKS source, an HSM/KMS) rather than sample keys.
4. **Secrets**: load connection strings, provider credentials, and signing material from a secret manager (the sample reads `appsettings` / env vars; `GM.Secrets` is wired for this).
5. **Password & hashing parameters**: review `PasswordHasher` work factors and the `PasswordPolicyOptions` against current guidance.
6. **Rate-limit & lockout tuning**: set per-route limits and lockout thresholds for your threat model; add CAPTCHA/step-up where appropriate.
7. **Provider validation**: the external/social sign-in path should fully validate the provider token (issuer, audience, signature) before provisioning.
8. **Headers & CORS**: add security headers (HSTS, CSP for any UI) and a locked-down CORS policy at the gateway.
9. **Monitoring**: alert on lockout spikes, 429 surges, token-introspection failures, and reconciliation drift.

## Related

- Decisions behind these choices: [architecture-decisions.md](architecture-decisions.md)
- Tenant isolation details: [tenancy.md](tenancy.md)
- Settings (lifetimes, lockout, rate limits): [configuration.md](configuration.md)
- Event contracts crossing service boundaries: [events.md](events.md)
- Extending the sample safely: [extending.md](extending.md)
- Visual flows and trust topology: the **GM Sample Flows** diagram set (edge routing, token validation, RBAC enforcement).
