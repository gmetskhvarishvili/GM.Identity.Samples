# Multi-tenancy & scope isolation

How the sample isolates data per tenant, why authentication is deliberately cross-tenant, and how visibility and scope hardening interact with it.

## The model

Tenant-scoped entities carry a **nullable `Guid? TenantId`**. It is nullable on purpose: some rows are global (reference data, consent documents that apply across tenants) and sign-in happens before a tenant is known.

Isolation is enforced centrally in `ApplicationDbContext`, not in each query:

- **Reads** — `modelBuilder.ApplyTenantQueryFilters(_currentActor)` adds a global query filter so every query returns only rows for the current actor's tenant (plus global rows).
- **Writes** — `ChangeTracker.StampTenants(_currentActor)` stamps new rows with the current tenant on `SaveChanges`, so callers never set `TenantId` by hand.

The current tenant comes from `ICurrentActor` (also the source of the actor fields on the audit log). In a request it is resolved from the caller's context; the design-time factory (`ApplicationDbContextFactory`) supplies `TenantId => null` so migrations run without a tenant.

## Why authentication is cross-tenant

At `/connect/*` the caller has presented credentials but **not** a proven tenant. So the authentication path resolves the user across all tenants by bypassing the filter:

```csharp
var user = await unitOfWork.UserRepository
    .Query(true, null)
    .IgnoreQueryFilters()                 // cross-tenant: no proven tenant yet
    .FirstOrDefaultAsync(x => x.UserName == request.UserName && ...);
```

Once authenticated, the tenant the caller acts under is **asserted per request** (via the request's tenant context, e.g. an `X-Tenant-Id` header surfaced through `ICurrentActor`). Every request after sign-in is therefore tenant-scoped again by the global filter.

> This is the one place the global filter is deliberately turned off. Treat `IgnoreQueryFilters()` as a red flag everywhere else — it is correct for authentication and a few admin cross-tenant operations, and a bug almost anywhere else.

## Visibility is orthogonal to tenancy

Tenancy answers *which tenant's rows*; **visibility** answers *which lifecycle states*. They compose. Reads default to visible rows within the current tenant; to see soft-deleted / inactive / hidden rows (e.g. an admin restoring something), a query opts in with `VisibilityScope`:

```csharp
x => x.Id == request.Id
  && (request.Visibility.HasFlag(VisibilityScope.IncludeInactive) || x.IsActive)
  && (request.Visibility.HasFlag(VisibilityScope.IncludeDeleted)  || !x.IsDeleted)
  && (request.Visibility.HasFlag(VisibilityScope.IncludeHidden)   || !x.IsHidden)
```

`VisibilityScope` is a `[Flags]` enum (`VisibleOnly`, `IncludeInactive`, `IncludeDeleted`, `IncludeHidden`, `All`). It never widens the tenant boundary — only the lifecycle states returned **within** the resolved tenant.

## Scope & permission isolation

Access-control data (roles, permissions, scopes, operations, clients and their joins) is tenant-scoped like everything else, so:

- A user's roles and a role's permissions are resolved within the tenant, and the RBAC projection in Redis is keyed so one tenant's grants never satisfy another's `[HasPermission]` check.
- Client scopes and scope→operation mappings are tenant-scoped, so what a client may request is evaluated against the tenant it operates in.

The `2fa-tenancy-scope-hardening` work tightens these edges: ensuring second-factor enrolment, scope evaluation, and the cache projections all respect the tenant boundary rather than leaking across it.

## Operational notes & gotchas

- **Global filter is on by default.** Any read that must cross tenants (authentication, certain platform-admin views, reconciliation) must call `IgnoreQueryFilters()` explicitly and justify it. Keep those call sites few and reviewed.
- **Null tenant = global.** A row with `TenantId == null` is visible to all tenants. Use it only for genuinely global data; never leave an operational row's tenant unset.
- **Stamping is automatic.** Don't set `TenantId` in command handlers — `StampTenants` does it on save from the current actor.
- **Migrations run tenant-less.** The design-time factory returns a null tenant, so the global filter and stamping are inert during `dotnet ef` operations — expected.
- **Audit carries the tenant.** The domain-event log records `TenantId` per event, so the action-history query can be filtered to a single tenant.

## Related

- Why this approach: ADR-009 in [architecture-decisions.md](architecture-decisions.md)
- How tenant isolation contributes to the security posture: [security.md](security.md)
- Settings reference: [configuration.md](configuration.md)
- Integration event contracts: [events.md](events.md)
- Extensibility how-tos: [extending.md](extending.md)
