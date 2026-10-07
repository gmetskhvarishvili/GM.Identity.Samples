# Extending the sample — how-to recipes

Task-oriented guides for the common ways you'll extend this stack. Each follows the patterns already in the code; file paths are relative to the repo root.

- [Promote a sample entity onto a GM.\* base](#promote-a-sample-entity-onto-a-gm-base)
- [Add an RBAC-protected endpoint](#add-an-rbac-protected-endpoint)
- [Add an OAuth grant type](#add-an-oauth-grant-type)
- [Add a second-factor (2FA) method](#add-a-second-factor-2fa-method)
- [Add a notification channel](#add-a-notification-channel)
- [Register a relying-party client](#register-a-relying-party-client)

See also: [adding an integration event](events.md#adding-an-event).

---

## Promote a sample entity onto a GM.\* base

The sample's aggregates inherit a `GM.*` base that owns their shape and behaviour (see [ADR-002](architecture-decisions.md#adr-002--reuse-gm-base-entities-by-inheritance)). This is how to move an entity that currently lives only in the sample into the library so other consumers can reuse it.

1. **Add the base entity** in the library (`GM.Identity.Domain`, under the right bounded context, e.g. `Authorization/XAggregate/Entities/GMX.cs`):
   ```csharp
   public class GMX : SoftDeletableEntity<Guid>
   {
       protected GMX() { }                       // EF materialization
       protected GMX(/* args */) { Id = Guid.NewGuid(); /* set props */ }
       public string SomeProp { get; private set; } = null!;
       // behaviour methods here; raise domain events if the aggregate has a lifecycle
   }
   ```
   Keep properties `public { get; private set; }`, constructors `protected`, and put behaviour on the base — not the sample subclass.

2. **Add the generic base configuration** in `GM.Identity.Persistence/Configuration/GMXConfiguration.cs`:
   ```csharp
   public class GMXConfiguration<TX>(string? schema, string table) : IEntityTypeConfiguration<TX>
       where TX : GMX
   {
       public virtual void Configure(EntityTypeBuilder<TX> builder)
       {
           builder.ToTable(table, schema);
           builder.HasKey(x => x.Id);
           // map the base's properties and indexes
       }
   }
   ```
   Make `Configure` **virtual** and `schema` nullable so a derived aggregate can add facets and use the model's default schema.

3. **Pack and reference.** Pack the library at a new lockstep version into the repo-local feed and bump the sample's package references (see [CONTRIBUTING.md](../CONTRIBUTING.md) for the `./nuget-local` workflow):
   ```bash
   dotnet pack GM.Identity.Domain/GM.Identity.Domain.csproj       -c Release -p:Version=<v> -o ./nuget-local
   dotnet pack GM.Identity.Persistence/GM.Identity.Persistence.csproj -c Release -p:Version=<v> -o ./nuget-local
   ```
   Pack Domain **and** Persistence at the same version (Persistence depends on Domain), and bump both `PackageReference`s in the sample. If you re-pack the same version, delete the stale copy from the global cache first (`~/.nuget/packages/<id>/<v>`).

4. **Make the sample aggregate a child** (`GM.Identity.Sample.Domain/.../XAggregate/X.cs`):
   ```csharp
   public class X : GMX, IAggregateRoot
   {
       private X() { }
       private X(/* args */) : base(/* args */) { }
       public static X Create(/* args */) => new(/* args */);
   }
   ```
   `IAggregateRoot` goes on the concrete type only.

5. **Reduce the sample configuration to a child** (`GM.Identity.Sample.Persistence/Configuration/XConfiguration.cs`):
   ```csharp
   public class XConfiguration() : GMXConfiguration<X>(null, "x_table");
   ```
   If the sample needs extra columns/indexes, override `Configure`, call `base.Configure(builder)`, then add them (as `UserTotpDeviceConfiguration` does).

6. **Build and migrate.** Build the solution; if the mapping changed, generate a migration and review it:
   ```bash
   dotnet ef migrations add PromoteX -p GM.Identity.Sample.Persistence -s GM.Identity.Sample.API
   ```
   A faithful promotion (same table/columns/indexes, unchanged concrete type name) produces **no** schema change.

---

## Add an RBAC-protected endpoint

Permission names are discovered from the `[HasPermission]` attributes on controller actions and seeded automatically (`GM.Identity.Sample.API/Program.cs` collects `HasPermissionAttribute.PermissionName` and passes them to `ApplicationDbContextSeed.SeedAsync`). Enforcement is a cache check on the hot path (see [ADR-003](architecture-decisions.md#adr-003--event-driven-cache-projection-not-inline-writes)).

1. **Protect the action** with a permission name:
   ```csharp
   [HasPermission(nameof(DoTheThing))]
   [HttpPost("{id}/Thing", Name = nameof(DoTheThing))]
   public Task<IActionResult> DoTheThing(...) => ...
   ```
   On next startup the seeder registers `DoTheThing` as a permission.

2. **Grant it to a role** — create the `RolePermission` (`POST /Roles/{id}/Permissions`). The command raises `GMRolePermissionCreatedDomainEvent`; the projection job adds it to the Redis RBAC cache.

3. **Assign the role to the user** (`POST /Users/{id}/Roles`), which projects the user→role link into the cache.

4. Calls now pass the `[HasPermission]` check without a database round-trip. For a brand-new **operation/scope** (OAuth scope authorization rather than RBAC), create the `Operation`, `Scope`, and `ScopeOperation` the same way — they project into the scope cache.

---

## Add an OAuth grant type

The token endpoint is a grant dispatcher in `GM.Identity.Sample.Application/Accounts/Commands/Authorize/AuthorizeCommand.cs`:

```csharp
return request.GrantType switch
{
    "refresh_token"      => await RefreshAsync(...),
    "two_factor"         => await TwoFactorGrantAsync(...),
    "authorization_code" => await AuthorizationCodeGrantAsync(...),
    "urn:ietf:params:oauth:grant-type:device_code" => await DeviceCodeGrantAsync(...),
    _                    => await PasswordGrantAsync(...),   // default
};
```

1. **Add a case** for your `grant_type` and a private `YourGrantAsync(...)` method alongside the others.
2. **Validate** the client (done before the switch) and whatever your grant requires, then mint a session with the shared `IssueUserSessionAsync(...)` helper so token hashing, the `UserSession` row, and the Redis session cache are handled consistently.
3. Add any request fields to `AuthorizeCommand` / the API model, and reject the grant for clients that aren't allowed to use it.

Keep tokens opaque and stored only as hashes (see [ADR-005](architecture-decisions.md#adr-005--opaque-tokens-stored-as-hashes-not-self-contained-jwts)).

---

## Add a second-factor (2FA) method

The sample supports two kinds of second factor: **delivered one-time codes** (contact OTP, keyed by `TwoFactorAuthType` reference data) and **authenticator-app TOTP** (`UserTotpDevice`). Passkeys are a separate passwordless path.

To add a new **contact-based** method (e.g. a new channel):

1. **Seed a `TwoFactorAuthType`** row for it (reference data). Users enrol via `SetUserTwoFactor` (`UserTwoFactorAuthType`).
2. **Resolve its destination.** The login flow resolves the user's contact via `TwoFactorSubject(user)` and issues a challenge by publishing `TwoFactorChallengeIssuedIntegrationEvent` (see [events.md](events.md)); the OTP service generates the code and the Notifications service delivers it over the chosen channel. Make sure the subject/destination for your method is available on the user and routed to the right channel.
3. **No verification code to write** — completion goes through the existing `two_factor` grant, which tries the contact OTP (`TryVerifyOtpAsync`) and TOTP in turn.

To add a fundamentally different factor (e.g. WebAuthn as a step-up), model it like the passkey aggregate and add its verify step to the `two_factor` grant.

---

## Add a notification channel

In `GM.Notifications.Samples`, each channel is an aggregate + a send command + a polling worker + a sender service. Follow the email/SMS pattern.

1. **Aggregate** — add `XNotification` (inherit the GM.Notifications base, like `EmailNotification`) with its channel-specific fields.
2. **Commands/queries** — `SendX` / `SendXBatch` / `CancelX` and `GetXById` / `GetXList`, mirroring the `Emails` folder. `SendX` stores the row as `Pending`.
3. **Sender service** — define `IXSenderService` and an implementation that talks to the provider; register it in Infrastructure (the existing ones are `IEmailSenderService`, `ISmsSenderService`, `IPushSenderService`, `ISlackSenderService`, `IWhatsAppSenderService`).
4. **Worker** — add an `XNotificationWorker : BackgroundService` that polls `Pending` rows for the channel, calls the sender, and marks `Sent`/`Failed`; register it with `AddHostedService<XNotificationWorker>()` in its host.
5. **API** — add the channel's controller (`batch` / `{id}` get / `{id}` delete).
6. **Event intake (optional)** — to have the channel react to an integration event, add the event type to the inbox `EventTypeMap` in `InboxProcessorWorker` and map it to `SendX`.

Provider credentials belong in a secret store, not `appsettings` (see [configuration.md](configuration.md)).

---

## Register a relying-party client

A client is created server-side with a hashed secret returned once.

1. **Register the client** — `POST /Clients` (`RegisterClient`) returns the `client_id` and the plaintext secret **once**; only the hash is stored. Rotate later with `RotateClientSecret`.
2. **Register redirect URIs** — add each `ClientRedirectUri`; the authorization flow only redirects to a verbatim match.
3. **Grant scopes** — add `ClientScope` rows for the scopes the client may request (`POST /Clients/{id}/Scopes`); these project into the scope cache.
4. **Integrate** — the client runs authorization-code + PKCE through the **GM Identity Gateway**: `POST /connect/authorize` → `POST /connect/token`, then validates tokens via `/connect/introspect` and reads claims from `/connect/userinfo`. See the authorization-code and token-validation flows in the **GM Sample Flows** diagrams.

---

## Related

- [architecture-decisions.md](architecture-decisions.md) · [security.md](security.md) · [tenancy.md](tenancy.md) · [configuration.md](configuration.md) · [events.md](events.md)
