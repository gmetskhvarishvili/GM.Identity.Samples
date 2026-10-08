# GM Sample Flows

Flows & dependencies across the GM Identity, OTP, and Notifications samples

Every flow this identity provider exposes — from a user's first sign-in to logout, plus the administration behind them. Each diagram traces one request path through the OAuth 2.0 / OpenID Connect endpoints at `/connect/*` and the account and RBAC endpoints under `/Users`, `/Roles`, `/Permissions`, `/Scopes`, and `/Clients`.

Opaque access and refresh tokens are stored only as hashes; a session is cached by its access-token hash for fast validation. One-time codes (email/SMS) are generated and delivered out of band by a notifier that consumes outbox events; authenticator-app (TOTP) and passkey proofs are verified in-process. RBAC writes don't touch Redis inline: the aggregate raises a domain event, and after the save the dispatcher projects it into (or evicts it from) the cache, directly or through a background job.

The provider doesn't send codes or messages itself. It publishes integration events that two sibling services consume: the **OTP sample** generates and verifies one-time codes, and the **Notifications sample** delivers email, SMS, push, Slack, and WhatsApp. Their flows and dependencies are documented alongside the provider's, with a cross-service view at the end.

## Participants

Identity API

the provider — endpoints at `/connect` and `/Users`

Database

users, sessions, codes, consents (PostgreSQL)

Session cache

Redis projection keyed by access-token hash

Outbox → Notifier

integration events that fan out to email/SMS

OTP service

generates and verifies delivered one-time codes

Authenticator

the user's TOTP app or WebAuthn device

## Flows

**Password & 2FA**

1. [Password sign-in](#password)
2. [Two-factor completion](#twofactor)
3. [Authenticator (TOTP) enrolment](#totp)

**OAuth / OIDC**

1. [Authorization code + PKCE](#authcode)
2. [Silent SSO authorization](#silent)
3. [Refresh token](#refresh)
4. [Device authorization grant](#device)
5. [Client credentials](#clientcreds)
6. [Discovery & keys](#discovery)

**Passkeys & social**

1. [Passkey registration](#passkey-reg)
2. [Passkey sign-in](#passkey-login)
3. [External / social sign-in](#external)

**Account lifecycle**

1. [Registration](#register)
2. [Account confirmation](#confirm)
3. [Password reset](#reset)

**Account self-service**

1. [Change password](#change-pw)
2. [Manage contact 2FA](#contact-2fa)
3. [Disable authenticator](#disable-totp)
4. [Accept a consent document](#accept-consent)

**Sessions**

1. [End session (single logout)](#logout)
2. [Token validation & revocation](#validate)

**Administration & RBAC**

1. [Permission enforcement](#enforce)
2. [Grant & revoke role permissions](#role-perm)
3. [Assign & remove user roles](#user-role)
4. [Scope ↔ operation mapping](#scope-op)
5. [Entity lifecycle](#lifecycle)
6. [Block or unlock a user](#block)
7. [Revoke a user's sessions](#revoke-sessions)
8. [Create, update & delete](#crud)
9. [Register client & rotate secret](#client)
10. [Grant & revoke client scopes](#client-scope)
11. [Revoke client sessions](#client-sessions)
12. [Delete a user](#delete-user)
13. [Publish a consent document](#publish-consent)
14. [RBAC cache reconciliation](#reconcile)

**Audit**

1. [Action history](#audit)

**Architecture**

1. [Edge: GM Identity Gateway](#edge-public)
2. [Edge: Admin gateway](#edge-admin)
3. [Gateway deps: public](#gw-public-deps)
4. [Gateway deps: admin](#gw-admin-deps)
5. [Project dependencies](#proj-deps)
6. [GM.\* package families](#pkg-deps)
7. [Runtime & external systems](#runtime-deps)
8. [C4 model (Structurizr)](#structurizr)

**OTP sample**

1. [Generate a code](#otp-generate)
2. [Verify a code](#otp-verify)
3. [Event-driven issuance](#otp-intake)
4. [Project dependencies](#otp-deps)

**Notifications sample**

1. [Queue a notification](#notif-send)
2. [Channel delivery](#notif-deliver)
3. [Cancel a queued message](#notif-cancel)
4. [Event-driven intake](#notif-intake)
5. [Project dependencies](#notif-deps)

**Ecosystem**

1. [Cross-service runtime](#ecosystem)

**Data model**

1. [Identity schema](#erd-identity)
2. [OTP & Notifications schema](#erd-services)

**State machines**

1. [Device code](#sm-devicecode)
2. [OTP challenge](#sm-otp)
3. [Notification](#sm-notification)
4. [Session](#sm-session)
5. [Entity visibility](#sm-softdelete)

**Domain model**

1. [Aggregate inheritance](#class-domain)

## Password & two-factor

### Password sign-in

POST /connect/token · grant_type=password

Authenticates the client, then the user. If the account has **any confirmed second factor**, the password alone returns a challenge instead of tokens; otherwise a session is issued. Failed attempts count toward a lockout.

```
sequenceDiagram
  autonumber
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant R as Session cache
  participant OB as Outbox → Notifier
  App->>GW: POST /connect/token (password)
  GW->>API: proxy
  API->>DB: load client, verify secret
  API->>DB: find user by username (cross-tenant)
  Note over API: reject if blocked or locked out
  API->>API: verify password hash
  alt password invalid
    API->>DB: increment failed count, lock at threshold
    API-->>GW: forward
    GW-->>App: 400 invalid_credentials
  else second factor enrolled
    opt contact OTP method
      API->>OB: TwoFactorChallengeIssued
      OB-->>App: code delivered to user
    end
    API-->>GW: forward
    GW-->>App: twoFactorRequired + methods
  else no second factor
    API->>DB: create UserSession (token hashes)
    API->>R: cache session by access hash
    API-->>GW: forward
    GW-->>App: access_token + refresh_token
  end
```

### Two-factor completion

POST /connect/token · grant_type=two_factor

Completes a challenged sign-in. The password is **re-verified** (the challenge carries no trust on its own), then the submitted code is checked against the authenticator (RFC 6238) or the delivered one-time code — whichever matches.

```
sequenceDiagram
  autonumber
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant OTP as OTP service
  participant R as Session cache
  App->>GW: POST /connect/token (two_factor) + code
  GW->>API: proxy
  API->>DB: load client + user, re-verify password
  API->>DB: load enrolled factors / TOTP secret
  alt TOTP code
    API->>API: verify code against secret (RFC 6238)
  else delivered OTP
    API->>OTP: verify delivered code
  end
  Note over API: reject if neither matches
  API->>DB: create UserSession
  API->>R: cache session
  API-->>GW: forward
  GW-->>App: access_token + refresh_token
```

### Authenticator (TOTP) enrolment

POST setup · POST confirm

Setup mints a fresh secret and an `otpauth://` URI to show as a QR code, storing a **pending** device. It does not gate login until a generated code confirms it.

```
sequenceDiagram
  autonumber
  participant U as User
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant AUTH as Authenticator app
  App->>GW: setup TOTP (userId)
  GW->>API: proxy
  API->>DB: drop any pending device
  API->>API: generate Base32 secret + otpauth URI
  API->>DB: store pending UserTotpDevice
  API-->>GW: forward
  GW-->>App: secret + QR
  U->>AUTH: scan QR
  AUTH-->>U: 6-digit code
  U->>App: enter code
  App->>GW: confirm TOTP (userId, code)
  GW->>API: proxy
  API->>DB: load pending device
  API->>API: verify code (RFC 6238)
  API->>DB: mark confirmed
  API-->>GW: forward
  GW-->>App: enrolled — TOTP now gates login
```

## OAuth 2.0 / OpenID Connect

### Authorization code + PKCE

POST /connect/authorize → POST /connect/token (authorization_code)

The browser-based flow. Authorize mints a single-use code bound to the client, redirect URI, PKCE challenge, and OIDC nonce; the token exchange verifies the **PKCE code verifier** before issuing a session and id_token.

```
sequenceDiagram
  autonumber
  participant C as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant R as Session cache
  Note over C: build code_verifier + code_challenge
  C->>GW: POST /connect/authorize (challenge, credentials)
  GW->>API: proxy
  API->>DB: verify client + redirect URI registered
  API->>API: authenticate user
  opt client requires consent
    API-->>GW: forward
    GW-->>C: consent_required
    C->>GW: re-submit with approval
    GW->>API: proxy
    API->>DB: save UserClientConsent
  end
  API->>DB: store AuthorizationCode (hashed) + nonce
  opt establish SSO
    API->>DB: create SsoSession
    API-->>GW: forward
    GW-->>C: set SSO cookie
  end
  API-->>GW: forward
  GW-->>C: authorization code + redirect
  C->>GW: POST /connect/token (authorization_code) + verifier
  GW->>API: proxy
  API->>DB: load code by hash, check unused, unexpired, redirect URI
  Note over API: PKCE — SHA256(verifier) equals stored challenge
  API->>DB: consume code, create UserSession (SSO link)
  API->>R: cache session
  API-->>GW: forward
  GW-->>C: access_token + refresh_token + id_token (nonce echoed)
```

### Silent SSO authorization

POST /connect/authorize · prompt=none

Once an SSO session exists, a **second** client can get a code without re-entering credentials — the browser presents the SSO cookie and the provider mints a code silently, or returns `login_required`.

```
sequenceDiagram
  autonumber
  participant B as Browser
  participant C2 as Second Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  B->>C2: open app (needs login)
  C2->>GW: POST /connect/authorize (prompt=none) + SSO cookie
  GW->>API: proxy
  API->>DB: look up SsoSession by cookie hash
  alt session active
    API->>DB: mint AuthorizationCode (no re-auth)
    API-->>GW: forward
    GW-->>C2: code + redirect
  else none or expired
    API-->>GW: forward
    GW-->>C2: login_required
  end
```

### Refresh token

POST /connect/token · grant_type=refresh_token

Rotates the session: the presented refresh token is matched by hash, the old session is **revoked and evicted**, and a fresh pair is issued — carrying the SSO link forward so single logout still reaches it.

```
sequenceDiagram
  autonumber
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant R as Session cache
  App->>GW: POST /connect/token (refresh_token)
  GW->>API: proxy
  API->>DB: verify client, find session by refresh hash
  Note over API: reject if revoked or expired
  API->>DB: revoke old session (rotation)
  API->>R: evict old access hash
  API->>DB: create new UserSession (keep SSO link)
  API->>R: cache new session
  API-->>GW: forward
  GW-->>App: new access_token + refresh_token
```

### Device authorization grant

POST /connect/device_authorization · /connect/device · token (device_code)

For input-constrained devices (RFC 8628). The device shows a short user code and polls; the user approves on a second screen. Polling returns `authorization_pending` / `slow_down` until approval.

```
sequenceDiagram
  autonumber
  participant D as Device
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant U as User (2nd screen)
  participant R as Session cache
  D->>GW: POST /connect/device_authorization (client creds)
  GW->>API: proxy
  API->>DB: store DeviceCode (hashed) + user_code [Pending]
  API-->>GW: forward
  GW-->>D: device_code, user_code, verification_uri, interval
  par user approves
    U->>GW: POST /connect/device (user_code, credentials)
    GW->>API: proxy
    API->>DB: verify user, approve DeviceCode
  and device polls
    loop every interval
      D->>GW: POST /connect/token (device_code)
      GW->>API: proxy
      alt still pending
        API-->>GW: forward
        GW-->>D: authorization_pending / slow_down
      else approved
        API->>DB: consume DeviceCode, create UserSession
        API->>R: cache session
        API-->>GW: forward
        GW-->>D: access_token + refresh_token
      else denied or expired
        API-->>GW: forward
        GW-->>D: access_denied / expired_token
      end
    end
  end
```

### Client credentials

POST /connect/token · grant_type=ClientCredentials

Machine-to-machine: no user, no refresh token. The client authenticates with its secret and receives an application access token.

```
sequenceDiagram
  autonumber
  participant App as Service Client
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  App->>GW: POST /connect/token (ClientCredentials)
  GW->>API: proxy
  API->>DB: verify client secret
  API-->>GW: forward
  GW-->>App: access_token (app context, no refresh)
```

### Discovery & keys

GET /.well-known/openid-configuration · /.well-known/jwks.json

How a client bootstraps: it reads the discovery document for endpoint URLs and capabilities, then fetches the **JWKS** signing keys it uses to validate id_token signatures.

```
sequenceDiagram
  autonumber
  participant C as Client
  participant API as Identity API
  participant GW as GM Identity Gateway
  C->>GW: GET /.well-known/openid-configuration
  GW->>API: proxy
  API-->>GW: forward
  GW-->>C: endpoints, grant types, scopes, jwks_uri
  C->>GW: GET /.well-known/jwks.json
  GW->>API: proxy
  API-->>GW: forward
  GW-->>C: signing public keys (JWKS)
  Note over C: cache keys to validate id_token signatures
```

## Passkeys & social

### Passkey registration

POST /Users/{id}/Passkeys

The authenticator creates a keypair; only the **public key** and credential id reach the server. The private key never leaves the device.

```
sequenceDiagram
  autonumber
  participant U as User
  participant B as Browser (WebAuthn)
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant AUTH as Authenticator
  U->>B: add a passkey
  B->>AUTH: navigator.credentials.create()
  AUTH-->>B: public key + credential id
  B->>App: send public key (SPKI) + credential id
  App->>GW: POST /Users/{id}/Passkeys
  GW->>API: proxy
  API->>DB: ensure credential not already registered
  API->>DB: store UserPasskey (public key only)
  API-->>GW: forward
  GW-->>App: passkey id
```

### Passkey sign-in

POST /connect/passkey/begin → /connect/passkey/complete

Begin issues a single-use challenge (5 min). Complete verifies the **ES256 assertion signature** against the stored public key, binds it to the issued challenge, records the signature counter, and mints a session. No password involved.

```
sequenceDiagram
  autonumber
  participant B as Browser (WebAuthn)
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant AUTH as Authenticator
  participant R as Session cache
  B->>GW: POST /connect/passkey/begin (username)
  GW->>API: proxy
  API->>DB: store single-use PasskeyChallenge (5 min)
  API-->>GW: forward
  GW-->>B: challenge
  B->>AUTH: navigator.credentials.get(challenge)
  AUTH-->>B: assertion (authenticatorData, clientData, signature)
  B->>GW: POST /connect/passkey/complete
  GW->>API: proxy
  API->>DB: load credential + live challenge
  Note over API: verify clientData + ES256 signature
  API->>DB: record sign-count, consume challenge, create UserSession
  API->>R: cache session
  API-->>GW: forward
  GW-->>B: access_token + refresh_token
```

### External / social sign-in

GET /connect/{provider} → POST /connect/{provider}/token

Delegates authentication to Google, Facebook, or another provider. On first sign-in the account is **auto-provisioned** from the provider's email, then a local session is issued.

```
sequenceDiagram
  autonumber
  participant B as Browser
  participant P as Provider
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant R as Session cache
  B->>P: sign in with provider + consent
  P-->>App: provider token
  App->>GW: POST /connect/{provider}/token (provider token)
  GW->>API: proxy
  API->>P: validate token, read email
  alt user exists
    API->>DB: load user
  else first time
    API->>DB: auto-create user from email
  end
  API->>DB: create UserSession
  API->>R: cache session
  API-->>GW: forward
  GW-->>App: access_token + refresh_token
```

## Account lifecycle

### Registration

POST /Users

Creates the account with a hashed password, optional roles and 2FA preferences, and records consent against the **current** document versions. A registration event fans out for welcome/notification.

```
sequenceDiagram
  autonumber
  participant Caller as Caller
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant OB as Outbox → Notifier
  Caller->>GW: POST /Users (username, email, password, roles, 2FA, consents)
  GW->>API: proxy
  API->>DB: ensure username + email unique
  API->>API: hash password
  API->>DB: create User (+ roles, 2FA prefs)
  opt consents supplied
    API->>DB: record UserConsent vs current documents
  end
  API->>OB: UserRegistered
  API-->>GW: forward
  GW-->>Caller: new user id
```

### Account confirmation

POST confirm-init → POST confirm

Init raises an event that makes the notifier generate and send a code; confirm verifies it and marks the account confirmed.

```
sequenceDiagram
  autonumber
  participant U as User
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant OB as Outbox → Notifier
  participant OTP as OTP service
  App->>GW: confirm-init (userId)
  GW->>API: proxy
  API->>OB: UserConfirmationInitiated
  OB->>OTP: generate + send code
  OTP-->>U: code (email/SMS)
  U->>App: enter code
  App->>GW: confirm (userId, code)
  GW->>API: proxy
  API->>OTP: verify code
  API->>DB: mark user confirmed
  API->>OB: UserConfirmed
  API-->>GW: forward
  GW-->>App: confirmed
```

### Password reset

POST reset-request → POST recover

A request raises an event to send a reset code; recovery verifies the code, sets the new password, and **revokes every existing session** so old tokens stop working.

```
sequenceDiagram
  autonumber
  participant U as User
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant OB as Outbox → Notifier
  participant OTP as OTP service
  U->>App: forgot password (email)
  App->>GW: reset-request (email)
  GW->>API: proxy
  API->>OB: PasswordResetRequested
  OB->>OTP: generate + send reset code
  OTP-->>U: reset code
  U->>App: enter code + new password
  App->>GW: recover (code, new password)
  GW->>API: proxy
  API->>OTP: verify reset code
  API->>API: hash new password
  API->>DB: update password, revoke all sessions
  API-->>GW: forward
  GW-->>App: password changed
```

## Account self-service

### Change password

update password

Setting a new password **revokes every existing session**, so other devices must sign in again with the new credentials.

```
sequenceDiagram
  autonumber
  participant U as User
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant JOB as Session eviction
  participant R as Session cache
  U->>GW: change password (new password)
  GW->>API: proxy
  API->>API: hash the new password
  API->>DB: update password, revoke all sessions
  DB-->>JOB: revoked events evict sessions
  JOB->>R: remove each session
  API-->>GW: forward
  GW-->>U: changed — re-authentication required
```

### Manage contact 2FA

set two-factor (typeId, enabled)

Turns a contact one-time-code method (email/SMS) on or off for the account. Enabling checks the method is a valid active type before recording the preference.

```
sequenceDiagram
  autonumber
  participant U as User
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  U->>GW: enable / disable a 2FA method (typeId)
  GW->>API: proxy
  alt enable
    API->>DB: verify method is active, add UserTwoFactorAuthType
  else disable
    API->>DB: remove the user's method
  end
  API-->>GW: forward
  GW-->>U: 200 — second factor updated
```

### Disable authenticator

disable TOTP

Removes the user's authenticator-app device. Idempotent — a no-op when none is enrolled.

```
sequenceDiagram
  autonumber
  participant U as User
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  U->>GW: disable authenticator (userId)
  GW->>API: proxy
  API->>DB: remove the user's TOTP device(s)
  API-->>GW: forward
  GW-->>U: 200 — TOTP no longer required
  Note over API: idempotent if none enrolled
```

### Accept a consent document

record consent (type, version)

Records that the user accepted a document, but only against its **current** version — a stale version is rejected so acceptance always points at live text.

```
sequenceDiagram
  autonumber
  participant U as User
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  U->>GW: accept consent (type, version)
  GW->>API: proxy
  API->>DB: confirm it matches the current version
  API->>DB: record UserConsent (user, type, version)
  API-->>GW: forward
  GW-->>U: 200 — acceptance stored
```

## Sessions

### End session (single logout)

POST /connect/endsession

Revokes the SSO session and **every app session spawned under it**, evicts each from the cache, and returns front-channel logout URIs so the other clients clear their own state.

```
sequenceDiagram
  autonumber
  participant B as Browser
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant DB as Database
  participant R as Session cache
  participant Apps as Other client apps
  B->>GW: POST /connect/endsession (SSO cookie)
  GW->>API: proxy
  API->>DB: find SsoSession by cookie hash, revoke
  API->>DB: revoke all UserSessions it spawned
  API->>R: evict each session from cache
  API-->>Apps: front-channel logout URIs
  API-->>GW: forward
  GW-->>B: post-logout redirect
```

### Token validation & revocation

/connect/introspect · /connect/userinfo · /connect/revoke

How a token is checked and retired. Validation reads the session (cache first, then database); revocation marks the session and evicts it so it fails validation immediately.

```
sequenceDiagram
  autonumber
  participant RS as Resource Server
  participant App as Client App
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant R as Session cache
  participant DB as Database
  RS->>GW: POST /connect/introspect (token)
  GW->>API: proxy
  API->>R: look up token hash
  opt cache miss
    API->>DB: look up session
  end
  API-->>GW: forward
  GW-->>RS: active, sub, client_id, exp
  App->>GW: GET /connect/userinfo (Bearer)
  GW->>API: proxy
  API->>R: validate access hash → session
  API-->>GW: forward
  GW-->>App: user claims (sub, email, ...)
  App->>GW: POST /connect/revoke (token)
  GW->>API: proxy
  API->>DB: revoke matching session
  API->>R: evict from cache
  API-->>GW: forward
  GW-->>App: 200 OK
```

## Administration & RBAC

### Permission enforcement

any \[HasPermission\] endpoint

How access control runs on every protected call. The bearer token is validated against the **session cache**, then the required permission is checked against the **RBAC projection** in Redis — no database round-trip on the hot path.

```
sequenceDiagram
  autonumber
  participant App as Caller (Bearer)
  participant API as Identity API
  participant GW as GM Identity Gateway
  participant R as Session cache
  participant RC as RBAC cache
  App->>GW: call a [HasPermission] endpoint
  GW->>API: proxy
  API->>R: validate access-token hash → session
  Note over API: 401 if no valid session
  API->>RC: does the user hold the required permission?
  alt granted
    API-->>GW: forward
    GW-->>App: 200 + result
  else missing
    API-->>GW: forward
    GW-->>App: 403 forbidden
  end
```

### Grant & revoke role permissions

POST /Roles/{id}/Permissions · DELETE …

The command only writes the `RolePermission` row and raises a domain event. After the save, the dispatcher hands the event to a handler that **triggers a background job** to project it into — or evict it from — the Redis RBAC cache. The write and the cache update are decoupled.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Background job
  participant RC as RBAC cache
  Admin->>GW: grant / revoke role permission
  GW->>API: proxy
  API->>DB: add or soft-remove RolePermission
  Note over API,DB: aggregate raises a domain event
  DB-->>EV: after SaveChanges, dispatch event
  EV->>JOB: trigger projection / eviction job
  alt granted
    JOB->>RC: add (role → permission)
  else revoked
    JOB->>RC: remove (role → permission)
  end
  API-->>GW: forward
  GW-->>Admin: 200 — cache updates asynchronously
```

### Assign & remove user roles

POST /Users/{id}/Roles · DELETE …

Same event-driven shape, with one asymmetry: an **assignment** is projected into the cache directly by the handler, while a **removal** goes through an eviction job.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Eviction job
  participant RC as RBAC cache
  Admin->>GW: assign / remove user role
  GW->>API: proxy
  API->>DB: add or soft-remove UserRole
  DB-->>EV: dispatch domain event after save
  alt assigned
    EV->>RC: add (user → role)
  else removed
    EV->>JOB: trigger eviction job
    JOB->>RC: remove (user → role)
  end
  API-->>GW: forward
  GW-->>Admin: 200
```

### Scope ↔ operation mapping

POST /Scopes/{id}/Operations · DELETE …

Mapping an operation into a scope (or removing it) drives the **scope cache** the same way — write the `ScopeOperation` row, raise the event, reproject in the background.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Background job
  participant SC as Scope cache
  Admin->>GW: map / unmap operation in scope
  GW->>API: proxy
  API->>DB: add or soft-remove ScopeOperation
  DB-->>EV: dispatch event after save
  EV->>JOB: trigger scope projection / eviction
  JOB->>SC: update (scope → operations)
  API-->>GW: forward
  GW-->>Admin: 200
```

### Entity lifecycle

activate · deactivate · hide · unhide · restore · delete

Every RBAC aggregate shares a soft-state lifecycle. Each transition is a method on the aggregate that **raises its own domain event**; transitions on a cached entity trigger a reprojection so the cache follows the row.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Projection jobs
  Note over API: Role · Permission · Scope · Operation · Client · User
  Admin->>GW: activate / deactivate / hide / unhide / restore / delete
  GW->>API: proxy
  API->>DB: apply soft-state change on aggregate
  Note over API,DB: each override raises its domain event
  DB-->>EV: dispatch events after save
  opt affects a cached projection
    EV->>JOB: trigger reprojection / eviction
  end
  API-->>GW: forward
  GW-->>Admin: 200
```

### Block or unlock a user

set-block · unlock

Blocking an account **revokes all its sessions** at the same time, so access stops immediately; unlocking just clears the failed-attempt lockout left by the sign-in flow.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant JOB as Session eviction
  participant R as Session cache
  alt block
    Admin->>GW: block user
    GW->>API: proxy
    API->>DB: mark blocked, revoke all sessions
    DB-->>JOB: dispatched events evict sessions
    JOB->>R: remove each session
  else unlock
    Admin->>GW: unlock user
    GW->>API: proxy
    API->>DB: clear lockout + failed count
  end
  API-->>GW: forward
  GW-->>Admin: 200
```

### Revoke a user's sessions

DELETE one session · DELETE all sessions

Revoking marks the session(s) in the database; the **session-revoked event** drives a job that evicts the token hash from Redis, so the next call with that token fails validation.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Session eviction job
  participant R as Session cache
  Admin->>GW: revoke one / all user sessions
  GW->>API: proxy
  API->>DB: Revoke() session(s)
  Note over API,DB: Revoke raises a session-revoked event
  DB-->>EV: dispatch after save
  EV->>JOB: trigger eviction job (token hash)
  JOB->>R: remove session from cache
  API-->>GW: forward
  GW-->>Admin: 200 — token rejected next call
```

### Create, update & delete

Role · Permission · Scope · Operation · Client

The shared shape for reference data. Creates enforce a unique name; deletes are **soft-removes**. Creates and removes raise domain events so any cached projection follows; list and detail reads use the standard paged query with visibility filters.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  alt create
    Admin->>GW: create (unique name)
    GW->>API: proxy
    API->>DB: insert aggregate
  else update
    Admin->>GW: update fields
    GW->>API: proxy
    API->>DB: apply changes
  else delete
    Admin->>GW: delete
    GW->>API: proxy
    API->>DB: soft-remove aggregate
  end
  Note over API,DB: creates and removes raise domain events
  DB-->>EV: dispatch after save (reproject if cached)
  API-->>GW: forward
  GW-->>Admin: 200
```

### Register client & rotate secret

POST /Clients · rotate-secret

A client secret is generated server-side, **hashed and salted**, and only the hash is stored — the plaintext is returned once. Rotation replaces the stored hash with a fresh secret.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  Admin->>GW: register client (+ redirect URIs)
  GW->>API: proxy
  API->>API: generate secret, hash + salt
  API->>DB: store Client (secret hash only) + redirect URIs
  API-->>GW: forward
  GW-->>Admin: client_id + secret (shown once)
  Note over Admin,API: later
  Admin->>GW: rotate client secret
  GW->>API: proxy
  API->>API: generate new secret, hash + salt
  API->>DB: replace stored hash
  API-->>GW: forward
  GW-->>Admin: new secret (shown once)
```

### Grant & revoke client scopes

POST /Clients/{id}/Scopes · DELETE …

Controls which scopes a client may request. Same event-driven projection: write the `ClientScope` row, raise the event, update the scope cache in the background.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Background job
  participant SC as Scope cache
  Admin->>GW: grant / revoke client scope
  GW->>API: proxy
  API->>DB: add or soft-remove ClientScope
  DB-->>EV: dispatch event after save
  EV->>JOB: trigger projection / eviction
  JOB->>SC: update (client → scopes)
  API-->>GW: forward
  GW-->>Admin: 200
```

### Revoke client sessions

DELETE one · DELETE all client sessions

Retires machine-to-machine sessions the same way user sessions are retired: `Revoke()` raises an event, and a job evicts the client session from the cache.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant EV as Domain dispatcher
  participant JOB as Client-session eviction
  participant R as Session cache
  Admin->>GW: revoke one / all client sessions
  GW->>API: proxy
  API->>DB: Revoke() client session(s)
  Note over API,DB: Revoke raises a client-session-revoked event
  DB-->>EV: dispatch after save
  EV->>JOB: trigger eviction job
  JOB->>R: remove client session from cache
  API-->>GW: forward
  GW-->>Admin: 200
```

### Delete a user

DELETE /Users/{id}

A **soft delete**: the row is retained but marked removed, every session is revoked, and a `UserDeleted` integration event goes out so downstream systems can react.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  participant R as Session cache
  participant OB as Outbox → downstream
  Admin->>GW: delete user
  GW->>API: proxy
  API->>DB: soft-remove user, revoke all sessions
  API->>R: sessions evicted (via revoked events)
  API->>OB: UserDeleted
  API-->>GW: forward
  GW-->>Admin: 200
```

### Publish a consent document

POST /ConsentDocuments · add version

Each version is an immutable row. Publishing a new version **supersedes the current one**; every prior version is kept so existing acceptances still point at the exact text accepted.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  alt first version
    Admin->>GW: create consent document (type, v1)
    GW->>API: proxy
    API->>DB: insert as current
  else new version
    Admin->>GW: add version (type, vN)
    GW->>API: proxy
    API->>DB: supersede current, insert vN as current
  end
  API-->>GW: forward
  GW-->>Admin: document id
  Note over DB: every version retained, one current per type
```

### RBAC cache reconciliation

scheduled job · every 10 minutes

A safety net for the event-driven projection. On a schedule the job reads the **desired** user→role and role→permission sets from the database, replaces them in Redis, and drops anything no longer present — repairing any drift from a missed event.

```
sequenceDiagram
  autonumber
  participant CRON as Scheduler
  participant JOB as Reconciliation job
  participant DB as Database
  participant RC as RBAC cache
  CRON->>JOB: every 10 minutes
  JOB->>DB: read desired user→role and role→permission sets
  JOB->>RC: replace each user's roles and each role's permissions
  JOB->>RC: drop users / roles no longer present
  Note over JOB,RC: repairs drift from any missed event
```

## Audit

### Action history

GET domain events · filtered, paged

Every domain event is persisted with its actor context, giving admins a queryable **action history** — for example everything a given user did. Reads filter across actor, session, tenant, channel, event type, and date range.

```
sequenceDiagram
  autonumber
  participant Admin as Admin
  participant API as Identity API
  participant GW as Admin Gateway
  participant DB as Database
  Admin->>GW: query domain events (filters)
  GW->>API: proxy
  Note over Admin,API: userId · clientId · sessionId · tenantId · ip · channel · eventType · dates
  API->>DB: read stored domain-event log (paged, newest first)
  API-->>GW: forward
  GW-->>Admin: action history page
```

## Architecture & dependencies

### Edge: GM Identity Gateway

GM.Identity.Sample.Gateway.API (YARP + GM.Gateway)

The public front door. `/connect/*` and registration pass through anonymously; the `/me` self routes require an opaque token and are **rewritten to owner-scoped backend endpoints** using the principal, so a caller only ever sees their own data. Every route is rate-limited and has the caller's actor claims forwarded as headers.

```
sequenceDiagram
  autonumber
  participant C as Client / User
  participant GW as GM Identity Gateway
  participant RL as Redis (rate limits)
  participant API as Identity API
  C->>GW: /connect/*, POST register, GET /me, GET /me/sessions
  GW->>RL: check the route's rate-limit policy
  alt over limit
    GW-->>C: 429 Too Many Requests
  else /connect/* or register (public)
    GW->>API: proxy (forward actor claims if present)
    API-->>GW: response
    GW-->>C: response
  else /me, /me/sessions (owner-scoped)
    GW->>GW: authenticate opaque token (user id, session id)
    GW->>GW: rewrite /me to the backend user endpoint for that user
    GW->>API: proxy with forwarded actor headers
    API-->>GW: response
    GW-->>C: response
  end
```

### Edge: Admin gateway

GM.Identity.Sample.Admin.Gateway.API (YARP + GM.Gateway)

The administrative front door for the RBAC and client-management routes (`/clients`, `/roles`, `/permissions`, `/scopes`, `/operations`, `/users`). Unlike the public gateway, **every admin route requires an opaque token** and satisfies the default authorization policy before it is proxied; rate limiting and actor forwarding apply here too.

```
sequenceDiagram
  autonumber
  participant A as Administrator
  participant GW as Admin Gateway
  participant RL as Redis (rate limits)
  participant API as Identity API
  A->>GW: /api/v1/{clients|roles|permissions|scopes|operations|users}
  GW->>RL: check the route's rate-limit policy
  alt over limit
    GW-->>A: 429 Too Many Requests
  else allowed
    GW->>GW: authenticate opaque token, enforce authorization policy
    alt not authenticated / not authorized
      GW-->>A: 401 / 403
    else authorized
      GW->>GW: forward actor claims as headers (user id, session id)
      GW->>API: proxy to the backend cluster
      API-->>GW: response
      GW-->>A: response
    end
  end
```

### Gateway dependencies: public

GM.Identity.Sample.Gateway.API package references

A standalone web host with no project references to the sample core — it depends only on packages and reaches the API over HTTP. GM.Gateway provides the YARP proxy; the Redis packages back rate limiting, caching, and locks; GM.Identity supplies opaque-token auth and actor forwarding.

```
flowchart LR
  GW["GM.Identity.Sample.Gateway.API"]
  GWY["GM.Gateway (YARP proxy)"]
  RL["GM.RateLimiting.Redis"]
  CA["GM.Caching.Redis"]
  DL["GM.DistributedLock.Redis"]
  SR["StackExchange.Redis"]
  ID["GM.Identity (auth, actor forwarding)"]
  HC["GM.HealthChecks (+ Caching, DistributedLock)"]
  OT["OpenTelemetry.* (traces, metrics)"]
  SW["Swashbuckle SwaggerUI"]
  GW --> GWY
  GW --> RL
  GW --> CA
  GW --> DL
  GW --> SR
  GW --> ID
  GW --> HC
  GW --> OT
  GW --> SW
  GW -. proxies over HTTP .-> API["Identity API"]
```

### Gateway dependencies: admin

GM.Identity.Sample.Admin.Gateway.API package references

The same package set as the public gateway — the difference is behavioral, not structural: this host wires GM.Identity's opaque-token authentication and a default authorization policy so **every** admin route is protected before it is proxied.

```
flowchart LR
  GW["GM.Identity.Sample.Admin.Gateway.API"]
  GWY["GM.Gateway (YARP proxy)"]
  RL["GM.RateLimiting.Redis"]
  CA["GM.Caching.Redis"]
  DL["GM.DistributedLock.Redis"]
  SR["StackExchange.Redis"]
  ID["GM.Identity (required auth + authorization)"]
  HC["GM.HealthChecks (+ Caching, DistributedLock)"]
  OT["OpenTelemetry.* (traces, metrics)"]
  SW["Swashbuckle SwaggerUI"]
  GW --> GWY
  GW --> RL
  GW --> CA
  GW --> DL
  GW --> SR
  GW --> ID
  GW --> HC
  GW --> OT
  GW --> SW
  GW -. proxies over HTTP .-> API["Identity API"]
```

### Project dependencies

solution project references

The sample follows clean-architecture layering: arrows point from a project to the projects it references, and every path leads inward to **Domain**, which references no other sample project. API composes the outer layers; Infrastructure and Persistence sit between it and the core.

Two more hosts aren't shown here because they hold no project references to the core: the public and admin **gateways** are standalone reverse proxies (GM.Gateway + Redis) that reach the API over HTTP at runtime — see the edge and runtime diagrams.

```
flowchart TD
  API["Sample.API"]
  INF["Sample.Infrastructure"]
  PER["Sample.Persistence"]
  APP["Sample.Application"]
  DOM["Sample.Domain"]
  COM["Sample.Common"]
  TST["Sample.Tests"]
  API --> INF
  API --> PER
  API --> APP
  INF --> APP
  INF --> PER
  INF --> DOM
  PER --> APP
  PER --> DOM
  APP --> COM
  APP --> DOM
  TST --> API
  TST --> DOM
```

### GM.\* package families

GM.Identity · GM.OTP · GM.Messaging · GM.EntityFramework

Each GM family ships a **Domain** and a **Persistence** package; the sample's own Domain and Persistence projects inherit their base entities and EF configurations from them. Within a family, Persistence depends on its Domain; across families, every Domain builds on `GM.EntityFramework.Domain` and every Persistence on `GM.EntityFramework.Persistence`.

```
flowchart TD
  SD["Sample.Domain"]
  SP["Sample.Persistence"]
  IDd["GM.Identity.Domain"]
  OTd["GM.OTP.Domain"]
  MSd["GM.Messaging.Domain"]
  EFd["GM.EntityFramework.Domain"]
  IDp["GM.Identity.Persistence"]
  OTp["GM.OTP.Persistence"]
  MSp["GM.Messaging.Persistence"]
  EFp["GM.EntityFramework.Persistence"]
  SD --> IDd
  SD --> OTd
  SD --> MSd
  SD --> EFd
  SP --> IDp
  SP --> OTp
  SP --> MSp
  SP --> EFp
  IDp --> IDd
  OTp --> OTd
  MSp --> MSd
  IDd --> EFd
  OTd --> EFd
  MSd --> EFd
  IDp --> EFp
  OTp --> EFp
  MSp --> EFp
```

Beyond these, the sample pulls `GM.API` / `GM.API.Application` (API scaffolding), `GM.Identity` (token, PKCE, WebAuthn, password helpers), `GM.Mediator`, `GM.HttpClient`, `GM.OTP` (TOTP service), `GM.Messaging`, `GM.Scheduling`, `GM.Secrets`, `GM.Caching.Redis`, `GM.DistributedLock.Redis`, `GM.HealthChecks.*`, `GM.Exceptions`, and `GM.Testing`.

### Runtime & external systems

deployed service dependencies

What the running service talks to. PostgreSQL is the system of record; Redis backs the session, RBAC, and scope caches plus distributed locks. Delivered codes go out through the outbox to a notifier; one-time-code verification calls an external OTP service, and social sign-in federates to the providers.

```
flowchart LR
  USERS["Users and client apps"] --> GWP["GM Identity Gateway (YARP)"]
  ADMIN["Administrators"] --> GWA["Admin gateway (YARP)"]
  GWP -->|rate limit + actor headers| API
  GWA -->|auth + rate limit + actor headers| API
  GWP --> REDIS
  GWA --> REDIS
  subgraph svc["Identity service"]
    API["Identity API"]
    JOBS["Background jobs (GM.Scheduling)"]
  end
  API --> PG[("PostgreSQL")]
  API --> REDIS[("Redis — sessions, RBAC, scopes, rate limits, locks")]
  JOBS --> REDIS
  JOBS --> PG
  API -->|GM.HttpClient| OTPAPI["External OTP service"]
  API -->|outbox, GM.Messaging| NOTIFY["Notifier → email/SMS"]
  API -->|OIDC| SOCIAL["Google, Facebook"]
  API -->|OTLP| OTEL["Telemetry collector"]
```

### C4 model (Structurizr)

gm-sample-ecosystem.dsl

The same architecture and flows are also modelled in **Structurizr DSL** (the C4 model), delivered as a `.dsl` file alongside this page. It renders in Structurizr Lite or structurizr.com — the Mermaid diagrams above stay the quick reference; the DSL is the single source you can regenerate every C4 view from.

The workspace contains: a **System Landscape**; a **System Context** and **Container** view for each of Identity, OTP, and Notifications (the Identity containers include both gateways); a **Component** view of the Identity API; a **Deployment** view (edge / app / data tiers) for the Production environment; and **dynamic views** for the flows, including an `EdgeRoutingPublic` and `EdgeRoutingAdmin` view for the two gateways (symmetric flows — the RBAC writes, the channels, CRUD/lifecycle — share one container-level view that names what it covers).

```
identity = softwareSystem "GM Identity" {
    gatewayPublic = container "GM Identity Gateway"     "..." "YARP + GM.Gateway"
    gatewayAdmin  = container "Admin Gateway"           "..." "YARP + GM.Gateway"
    identityApi   = container "Identity API"           "..." "ASP.NET Core"
    identityJobs  = container "Background Jobs"         "..." "GM.Scheduling"
    identityDb    = container "Identity Database"       "..." "PostgreSQL" "Database"
    identityCache = container "Session / RBAC Cache"    "..." "Redis" "Cache"
}

clientApp     -> gatewayPublic "HTTPS (/connect, register, /me)"
gatewayPublic -> identityApi   "Proxies (rate-limited, actor-forwarded)"

identityApi -> bus       "Publishes integration events (outbox)"
identityApi -> otpApi    "Verifies submitted codes" "HTTPS / JSON"
bus         -> otpInbox  "Delivers Identity events"
bus         -> notifInbox "Delivers Identity & OTP events"

dynamic * "Ecosystem" "Identity publishes, OTP issues codes, Notifications delivers." {
    clientApp   -> identityApi  "User action needing a code"
    identityApi -> bus          "Publish event"
    bus         -> otpInbox     "Deliver to OTP"
    otpProducer -> bus          "Publish OtpGenerated"
    bus         -> notifInbox   "Deliver to Notifications"
    notifChannels -> providers  "Deliver to user"
}
```

Render locally: `docker run -it --rm -p 8080:8080 -v "$PWD":/usr/local/structurizr structurizr/lite` in the folder holding the `.dsl`, then open `localhost:8080`.

## GM.OTP sample

### Generate a code

POST /otp/generate

Supersedes any active challenge for the subject, then stores a fresh one — only the code's **hash**, with an expiry and an attempt limit. The raw code is delivered out of band (it is not returned in the response).

```
sequenceDiagram
  autonumber
  participant Caller as Caller (e.g. Identity)
  participant API as OTP API
  participant DB as Database
  participant M as OTP manager
  Caller->>API: POST /otp/generate (subject, destination, purpose)
  API->>DB: supersede active challenges for subject
  API->>M: generate code + hash
  API->>DB: store OtpChallenge (hash, expiry, max attempts)
  API-->>Caller: challenge issued
  Note over API,DB: raw code delivered out of band, never stored
```

### Verify a code

POST /otp/verify

The call the Identity provider makes to check a submitted code. The manager compares the hash in constant time and enforces the **attempt and expiry limits**; the challenge is updated either way.

```
sequenceDiagram
  autonumber
  participant Caller as Caller (e.g. Identity)
  participant API as OTP API
  participant DB as Database
  participant M as OTP manager
  Caller->>API: POST /otp/verify (subject, purpose, code)
  API->>DB: load active challenge (not used, not invalidated)
  Note over API: fail if none active
  API->>M: verify code (hash, attempts, expiry)
  API->>DB: mark used or count the failure
  API-->>Caller: valid / invalid
```

### Event-driven issuance

inbox worker · consumes Identity events

How codes get sent without the provider knowing about delivery. A worker reads the inbox for the provider's events, generates a challenge, and enqueues an `OtpGenerated` event carrying the code and destination for the Notifications service to deliver.

```
sequenceDiagram
  autonumber
  participant ID as Identity service
  participant BUS as Message bus / inbox
  participant W as OTP inbox worker
  participant H as GenerateOtp handler
  participant DB as Database
  participant NF as Notifications
  ID->>BUS: UserConfirmationInitiated / TwoFactorChallengeIssued / OtpRequested
  W->>BUS: read inbox (idempotent by event id)
  W->>H: GenerateOtp (subject, destination, purpose)
  H->>DB: store OtpChallenge (hashed)
  H->>BUS: enqueue OtpGenerated (code + destination)
  BUS-->>NF: delivered to Notifications
```

### Project dependencies

GM.OTP.Samples solution

Same clean-architecture layering as the provider, with three hosts over the core: the API, a producer worker (publishes the outbox), and inbox/consumer workers. It builds on the `GM.OTP.*` and `GM.Messaging.*` package families.

```
flowchart TD
  API["Sample.API"]
  INF["Sample.Infrastructure"]
  PER["Sample.Persistence"]
  APP["Sample.Application"]
  DOM["Sample.Domain"]
  COM["Sample.Common"]
  PW["Producer.Worker"]
  CW["Consumer.Worker"]
  IW["Inbox Worker"]
  API --> INF
  API --> PER
  API --> APP
  INF --> APP
  INF --> COM
  PER --> APP
  PER --> DOM
  APP --> COM
  APP --> DOM
  PW --> INF
  PW --> PER
  CW --> INF
  CW --> PER
  CW --> DOM
  IW --> INF
  IW --> PER
```

## GM.Notifications sample

### Queue a notification

POST /{channel}/batch

The API only **enqueues**: it stores one or more notification rows as *Pending* and returns their ids. Delivery is a separate step handled by the channel workers. Channels: email, SMS, push, Slack, WhatsApp.

```
sequenceDiagram
  autonumber
  participant Caller as Caller
  participant API as Notifications API
  participant DB as Database
  Caller->>API: POST /{channel}/batch (messages)
  API->>DB: store Notification rows [Pending]
  API-->>Caller: notification ids
  Note over API: email · SMS · push · Slack · WhatsApp
```

### Channel delivery

per-channel background worker

Each channel runs its own worker that polls for *Pending* rows, hands each to the channel's provider, and records the outcome — so a failing provider doesn't block the others.

```
sequenceDiagram
  autonumber
  participant W as Channel worker
  participant DB as Database
  participant P as Provider (SMTP / SMS / push / Slack / WhatsApp)
  loop poll interval
    W->>DB: fetch pending notifications for this channel
    loop each
      W->>P: send message
      alt sent
        W->>DB: mark Sent
      else error
        W->>DB: mark Failed (retried later)
      end
    end
  end
```

### Cancel a queued message

DELETE /{channel}/{id}

Cancels a notification while it is still pending; once a worker has sent it, the call is a no-op.

```
sequenceDiagram
  autonumber
  participant Caller as Caller
  participant API as Notifications API
  participant DB as Database
  Caller->>API: DELETE /{channel}/{id}
  API->>DB: cancel if still pending
  API-->>Caller: 200 (no-op if already sent)
```

### Event-driven intake

inbox worker · consumes Identity & OTP events

How the provider's and OTP service's events become messages. A worker reads the inbox and turns each event into a queued notification on the right channel — for example `OtpGenerated` into an email or SMS carrying the code.

```
sequenceDiagram
  autonumber
  participant SRC as Identity / OTP service
  participant BUS as Message bus / inbox
  participant W as Notifications inbox worker
  participant H as Send-channel handler
  participant DB as Database
  SRC->>BUS: UserRegistered / UserConfirmed / OtpGenerated
  W->>BUS: read inbox (idempotent by event id)
  W->>H: Send on the chosen channel (recipient, content)
  H->>DB: store Notification [Pending]
  Note over DB: a channel worker then delivers it
```

### Project dependencies

GM.Notifications.Samples solution

Core layering plus one host **per channel** (email, SMS, push, Slack, WhatsApp) and the inbox/consumer workers. Infrastructure pulls the `GM.Notifications.*` provider packages for each channel.

```
flowchart TD
  API["Sample.API"]
  INF["Sample.Infrastructure"]
  PER["Sample.Persistence"]
  APP["Sample.Application"]
  DOM["Sample.Domain"]
  COM["Sample.Common"]
  CH["Channel workers (Email, SMS, Push, Slack, WhatsApp)"]
  IW["Inbox / Consumer workers"]
  API --> INF
  API --> PER
  API --> APP
  INF --> DOM
  PER --> APP
  PER --> DOM
  APP --> COM
  APP --> DOM
  CH --> INF
  CH --> PER
  IW --> INF
  IW --> PER
  IW --> DOM
```

## Ecosystem

### Cross-service runtime

Identity · OTP · Notifications

How the three services fit together. The provider publishes events and never sends anything itself; the OTP service turns code requests into an `OtpGenerated` event; the Notifications service consumes events and delivers over its channels. The one direct call is the provider verifying a submitted code against the OTP API.

```
flowchart LR
  IDAPI["Identity service"]
  OTAPI["OTP service"]
  NFAPI["Notifications service"]
  BUS[("Message bus (outbox / inbox)")]
  PROV["Email · SMS · Push · Slack · WhatsApp providers"]
  IDAPI -->|UserConfirmationInitiated, TwoFactorChallengeIssued, OtpRequested| BUS
  IDAPI -->|UserRegistered, UserConfirmed, UserDeleted| BUS
  BUS --> OTAPI
  BUS --> NFAPI
  OTAPI -->|OtpGenerated| BUS
  IDAPI -->|verify code over HTTP| OTAPI
  NFAPI --> PROV
```

## Data model

### Identity schema

PostgreSQL · schema application / compliance

The Identity service's tables and how they relate. Keys are GUIDs; join tables carry the business keys the caches and queries look up by. Every row is soft-deletable (IsActive / IsDeleted / IsHidden), omitted here for clarity.

```
erDiagram
  USER ||--o{ USER_ROLE : has
  ROLE ||--o{ USER_ROLE : grants
  ROLE ||--o{ ROLE_PERMISSION : grants
  PERMISSION ||--o{ ROLE_PERMISSION : in
  SCOPE ||--o{ SCOPE_OPERATION : groups
  OPERATION ||--o{ SCOPE_OPERATION : in
  CLIENT ||--o{ CLIENT_SCOPE : may_request
  SCOPE ||--o{ CLIENT_SCOPE : granted_to
  CLIENT ||--o{ CLIENT_REDIRECT_URI : registers
  USER ||--o{ USER_TWO_FACTOR : enrols
  TWO_FACTOR_TYPE ||--o{ USER_TWO_FACTOR : of
  USER ||--o{ USER_SESSION : owns
  CLIENT ||--o{ USER_SESSION : for
  SSO_SESSION ||--o{ USER_SESSION : spawns
  USER ||--o{ SSO_SESSION : authenticates
  CLIENT ||--o{ CLIENT_SESSION : owns
  CLIENT ||--o{ AUTH_CODE : issued_for
  USER ||--o{ AUTH_CODE : for
  CLIENT ||--o{ DEVICE_CODE : issued_for
  USER ||--o{ USER_CLIENT_CONSENT : grants
  CLIENT ||--o{ USER_CLIENT_CONSENT : for
  USER ||--o{ USER_PASSKEY : registers
  USER ||--o{ PASSKEY_CHALLENGE : for
  USER ||--o{ USER_TOTP_DEVICE : enrols
  USER ||--o{ USER_CONSENT : accepts
  CONSENT_DOCUMENT ||--o{ USER_CONSENT : accepted_as
  USER {
    guid Id PK
    string UserName
    string Email
    string PasswordHash
    bool IsBlocked
    datetime LockoutEnd
  }
  ROLE {
    guid Id PK
    string Name
  }
  PERMISSION {
    guid Id PK
    string Name
  }
  ROLE_PERMISSION {
    guid Id PK
    guid RoleId FK
    guid PermissionId FK
  }
  USER_ROLE {
    guid Id PK
    guid UserId FK
    guid RoleId FK
  }
  SCOPE {
    guid Id PK
    string Name
  }
  OPERATION {
    guid Id PK
    string Name
  }
  SCOPE_OPERATION {
    guid Id PK
    guid ScopeId FK
    guid OperationId FK
  }
  CLIENT {
    guid Id PK
    string Name
    string SecretHash
  }
  CLIENT_SCOPE {
    guid Id PK
    guid ClientId FK
    guid ScopeId FK
  }
  CLIENT_REDIRECT_URI {
    guid Id PK
    guid ClientId FK
    string Uri
  }
  TWO_FACTOR_TYPE {
    int Id PK
    string Name
  }
  USER_TWO_FACTOR {
    guid Id PK
    guid UserId FK
    int TwoFactorAuthTypeId FK
  }
  USER_SESSION {
    guid Id PK
    guid UserId FK
    guid ClientId FK
    string TokenHash
    guid SsoSessionId FK
  }
  CLIENT_SESSION {
    guid Id PK
    guid ClientId FK
    string TokenHash
  }
  SSO_SESSION {
    guid Id PK
    guid UserId FK
    string TokenHash
    datetime ExpiresAt
    datetime RevokedAt
  }
  AUTH_CODE {
    guid Id PK
    guid ClientId FK
    guid UserId FK
    string CodeHash
    string CodeChallenge
    guid SsoSessionId FK
  }
  DEVICE_CODE {
    guid Id PK
    guid ClientId FK
    string DeviceCodeHash
    string UserCode
    int Status
    guid UserId FK
  }
  USER_CLIENT_CONSENT {
    guid Id PK
    guid UserId FK
    guid ClientId FK
    string Scopes
  }
  USER_PASSKEY {
    guid Id PK
    guid UserId FK
    string CredentialId
    blob PublicKeySpki
    long SignCount
  }
  PASSKEY_CHALLENGE {
    guid Id PK
    guid UserId FK
    string Challenge
    datetime ExpiresAt
  }
  USER_TOTP_DEVICE {
    guid Id PK
    guid UserId FK
    string SecretBase32
    bool IsConfirmed
  }
  CONSENT_DOCUMENT {
    guid Id PK
    string ConsentType
    string Version
    bool IsCurrent
  }
  USER_CONSENT {
    guid Id PK
    guid UserId FK
    string ConsentType
    string DocumentVersion
  }
```

### OTP & Notifications schema

separate databases · linked only by events

The sibling services keep their own stores — there are no cross-service foreign keys; they're joined only by the integration events on the bus. Each also has the outbox/inbox tables that carry those events.

```
erDiagram
  OTP_CHALLENGE {
    guid Id PK
    string Subject
    string Destination
    string CodeHash
    string Purpose
    int FailedAttempts
    int MaxAttempts
    bool IsUsed
    bool IsInvalidated
    datetime ExpiresAtUtc
  }
  NOTIFICATION {
    guid Id PK
    string Channel
    string Recipient
    int Status
    int RetryCount
    int MaxRetries
    datetime SentAtUtc
    guid UserId
  }
  OUTBOX_MESSAGE {
    guid Id PK
    string Type
    string Payload
    string CorrelationId
    datetime ProcessedAt
  }
  INBOX_MESSAGE {
    guid Id PK
    string EventId
    string Type
    datetime ProcessedAt
  }
```

Notification is one table per channel in the real schema (email, SMS, push, Slack, WhatsApp), each with the same status shape shown here.

## State machines

### Device code

DeviceCodeStatus

A device-authorization request moves out of **Pending** only by the user's choice, and is consumed when the device finally exchanges it.

```
stateDiagram-v2
  [*] --> Pending : issued
  Pending --> Approved : Approve(userId)
  Pending --> Denied : Deny()
  Approved --> [*] : consumed at token exchange
  Pending --> Expired : past ExpiresAt
  Approved --> Expired : past ExpiresAt
  Denied --> [*]
  Expired --> [*]
```

### OTP challenge

IsUsed · IsInvalidated · expiry

A delivered code is active until it is used, superseded or attempt-exhausted (invalidated), or expires. Verification is single-use.

```
stateDiagram-v2
  [*] --> Active : generated
  Active --> Used : correct code
  Active --> Invalidated : max attempts or superseded
  Active --> Expired : past ExpiresAtUtc
  Used --> [*]
  Invalidated --> [*]
  Expired --> [*]
```

### Notification

NotificationStatus

Queued as **Pending**, delivered by the channel worker. A failure retries until the retry budget is spent; a pending message can be cancelled.

```
stateDiagram-v2
  [*] --> Pending : queued
  Pending --> Sent : delivered by worker
  Pending --> Failed : provider error
  Failed --> Pending : retry (under MaxRetries)
  Failed --> [*] : retries exhausted
  Pending --> Cancelled : cancelled while pending
  Sent --> [*]
  Cancelled --> [*]
```

### Session

UserSession · ClientSession · SsoSession

Every session type shares the same shape: active until revoked (logout, refresh rotation, or admin) or expired. Revocation is what the cache eviction jobs act on.

```
stateDiagram-v2
  [*] --> Active : issued
  Active --> Revoked : logout / rotation / admin
  Active --> Expired : past expiry
  Revoked --> [*]
  Expired --> [*]
```

### Entity visibility

SoftDeletableEntity

The soft-state lifecycle every aggregate inherits. The three flags are independent — this shows the primary transitions each method drives; an admin can restore from any of them.

```
stateDiagram-v2
  [*] --> Active : created
  Active --> Inactive : Deactivate()
  Inactive --> Active : Activate()
  Active --> Hidden : Hide()
  Hidden --> Active : Unhide()
  Active --> Deleted : SoftRemove()
  Deleted --> Active : RestoreAll()
```

## Domain model

### Aggregate inheritance

GM.\* base entities ↔ sample aggregates

The inheritance the sample is built on: every GM.\* base entity extends `SoftDeletableEntity` and owns the shape and behaviour (raising domain events); each sample type fixes it as the application's aggregate by subclassing the base and implementing `IAggregateRoot`. `TotpEnrolment` comes from GM.OTP; the rest from GM.Identity.

```
classDiagram
  class SoftDeletableEntity {
    <>
    +Guid Id
    +bool IsActive
    +bool IsDeleted
    +bool IsHidden
    +Activate()
    +Deactivate()
    +Hide()
    +Unhide()
    +SoftRemove()
    +RestoreAll()
  }
  class IAggregateRoot {
    <>
  }
  class GMUser {
    +string UserName
    +string PasswordHash
    +UpdatePassword()
    +Block()
    +Unlock()
  }
  class TotpEnrolment {
    +string SecretBase32
    +bool IsConfirmed
    +Confirm()
  }
  SoftDeletableEntity <|-- GMUser
  SoftDeletableEntity <|-- GMConsentDocument
  SoftDeletableEntity <|-- GMUserConsent
  SoftDeletableEntity <|-- GMUserPasskey
  SoftDeletableEntity <|-- GMPasskeyChallenge
  SoftDeletableEntity <|-- GMAuthorizationCode
  SoftDeletableEntity <|-- GMClientRedirectUri
  SoftDeletableEntity <|-- GMDeviceCode
  SoftDeletableEntity <|-- GMSsoSession
  SoftDeletableEntity <|-- GMUserClientConsent
  SoftDeletableEntity <|-- TotpEnrolment
  GMUser <|-- User
  GMConsentDocument <|-- ConsentDocument
  GMUserConsent <|-- UserConsent
  GMUserPasskey <|-- UserPasskey
  GMPasskeyChallenge <|-- PasskeyChallenge
  GMAuthorizationCode <|-- AuthorizationCode
  GMClientRedirectUri <|-- ClientRedirectUri
  GMDeviceCode <|-- DeviceCode
  GMSsoSession <|-- SsoSession
  GMUserClientConsent <|-- UserClientConsent
  TotpEnrolment <|-- UserTotpDevice
  IAggregateRoot <|.. User
  IAggregateRoot <|.. ConsentDocument
  IAggregateRoot <|.. UserConsent
  IAggregateRoot <|.. UserPasskey
  IAggregateRoot <|.. PasskeyChallenge
  IAggregateRoot <|.. AuthorizationCode
  IAggregateRoot <|.. ClientRedirectUri
  IAggregateRoot <|.. DeviceCode
  IAggregateRoot <|.. SsoSession
  IAggregateRoot <|.. UserClientConsent
  IAggregateRoot <|.. UserTotpDevice
```

Opaque tokens are stored and matched only as hashes (`TokenGenerator.Hash`); the session cache is a Redis projection keyed by the access-token hash. Delivered one-time codes travel through outbox integration events to a notifier; TOTP and WebAuthn proofs are verified in-process. RBAC changes raise domain events that project into the Redis RBAC and scope caches after the save — directly or via a background job — with a reconciliation job repairing drift on a schedule. Diagrams reflect the handlers under `Accounts`, `Users`, `Roles`, `Permissions`, `Scopes`, and `Clients` commands, plus the projection jobs under `Infrastructure/Authorization`. The OTP and Notifications sections reflect `GM.OTP.Samples` (generate/verify handlers and the inbox worker) and `GM.Notifications.Samples` (per-channel send handlers and the channel + inbox workers).