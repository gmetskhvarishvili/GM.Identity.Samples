# Architecture decision records

The deliberate design choices this sample demonstrates, and the trade-offs behind them. Each record is lightweight: **Context → Decision → Consequences**. These describe *why* the code is shaped the way it is; the *what* lives in the code and the diagram set.

Status legend: **Accepted** — in effect in the sample.

---

## ADR-001 — Clean architecture with bounded contexts

**Status:** Accepted

**Context.** The sample models a non-trivial domain (identity, access control, authorization, compliance, messaging) that must stay testable and must not let infrastructure concerns leak into business rules.

**Decision.** Layer the solution so dependencies point inward to `Domain`: `Domain` ← `Application` ← `Persistence` / `Infrastructure` ← `API`. Group aggregates by bounded context (Identity, AccessControl, Authorization, Compliance, Messaging, Audit) rather than by technical type.

**Consequences.** Business rules live in aggregates and are unit-testable without a database. The outer layers are swappable. The cost is more projects and more mapping (commands/queries/DTOs/models) than a flat design — accepted because the sample exists to demonstrate the structure.

---

## ADR-002 — Reuse GM.\* base entities by inheritance

**Status:** Accepted

**Context.** The GM.Identity / GM.OTP libraries ship base entities (`GMUser`, `GMUserPasskey`, `GMConsentDocument`, `TotpEnrolment`, …) and matching EF configurations. The sample needs concrete aggregates without re-implementing shape and behaviour.

**Decision.** Each sample aggregate subclasses the library base and marks itself `IAggregateRoot`; the base owns the properties, invariants, and domain events, while the sample type fixes it as the application's aggregate and exposes the factory. Persistence configurations subclass the generic library configuration (`GMUserPasskeyConfiguration<T>`, `TotpEnrolmentConfiguration<T>`, …), passing the table/schema.

**Consequences.** New capabilities added to a library base flow into the sample for free, and the mapping is declared once. `IAggregateRoot` stays on the concrete type only. The cost is an inheritance coupling to the library's shape; when the sample needs extra columns or indexes, the child configuration overrides `Configure` and adds them (as `UserTotpDevice` does).

---

## ADR-003 — Event-driven cache projection, not inline writes

**Status:** Accepted

**Context.** RBAC and scope decisions are served from Redis projections for speed. Writing to Redis inline inside each command handler couples the write model to the cache, risks partial failure (DB committed, cache not), and scatters cache logic.

**Decision.** Command handlers only persist the aggregate and raise a domain event. After `SaveChanges`, the dispatcher hands the event to a handler that updates the cache — directly for a simple add, or by triggering a background job for an eviction/reprojection. The write model never calls the cache.

**Consequences.** The write path is simple and transactional; cache updates are decoupled and retryable. The trade-off is **eventual consistency** — there is a short window after a commit before the projection reflects it. That window is bounded by the jobs and backstopped by reconciliation (ADR-004). Auth-flow session writes that must be immediately usable (e.g. caching a new session) are the deliberate exception and stay synchronous.

---

## ADR-004 — A reconciliation job backstops the projections

**Status:** Accepted

**Context.** Event-driven projection (ADR-003) can drift if an event is missed (crash between commit and dispatch, a bug, a manual DB change).

**Decision.** A scheduled job (`permission-cache-reconcile`, every ~10 minutes) reads the desired user→role and role→permission sets from the database, replaces them in Redis, and drops entries no longer present.

**Consequences.** The cache is self-healing; drift is corrected without a redeploy or manual flush. The cost is a periodic full read of the RBAC sets — acceptable at the sample's scale, and the interval is configurable for larger ones.

---

## ADR-005 — Opaque tokens stored as hashes (not self-contained JWTs)

**Status:** Accepted

**Context.** Access/refresh tokens must be revocable immediately (logout, rotation, block, delete), and a leaked token store must not hand an attacker usable credentials.

**Decision.** Issue opaque random tokens. Store only `TokenGenerator.Hash(token)`. Validate by looking the hash up in the session store (Redis first, database second). (OIDC `id_token`s are still signed JWTs, because relying parties verify them offline.)

**Consequences.** Revocation is instant and a stolen database yields no replayable tokens. The cost is a lookup per validation instead of offline signature verification — mitigated by the Redis projection. Resource servers validate via `/connect/introspect` rather than local JWT checks.

---

## ADR-006 — Outbox/inbox with separate OTP and Notifications services

**Status:** Accepted

**Context.** Sending one-time codes and notifications is slow, failure-prone, and orthogonal to issuing identity. Doing it inline would couple login latency to an SMTP/SMS provider.

**Decision.** The Identity service publishes integration events through an **outbox** (written in the same transaction as the state change). Separate services consume them: the **OTP service** generates/verifies codes and re-publishes `OtpGenerated`; the **Notifications service** queues and delivers across channels. Consumers are idempotent via an **inbox** keyed on the event id.

**Consequences.** Identity stays fast and has no provider dependencies; delivery scales and fails independently; exactly-once-ish processing via the inbox. The cost is operational — three deployables, a broker, and eventual delivery — accepted to demonstrate the decoupled, event-driven shape.

---

## ADR-007 — Two gateways: public and admin

**Status:** Accepted

**Context.** Public OAuth/OIDC traffic and privileged administration have different exposure, auth, and rate-limit needs, and the API should not be reachable directly.

**Decision.** Front the API with two YARP gateways (built on `GM.Gateway`). The **public gateway** exposes `/connect/*`, registration, and owner-scoped `/me` routes (anonymous where appropriate, authenticated and rewritten for `/me`). The **admin gateway** requires an opaque token and a default authorization policy on **every** RBAC/admin route. Both rate-limit per route and forward actor claims to the backend.

**Consequences.** Clear separation of public vs privileged surface; the backend trusts the gateway's forwarded actor context; admin endpoints are protected uniformly. The cost is two more hosts and the config to keep their routes in sync with the API.

---

## ADR-008 — Soft delete everywhere, with explicit visibility opt-in

**Status:** Accepted

**Context.** Identity data (users, clients, consents, RBAC) is rarely safe to destroy, and admins need to deactivate, hide, restore, and audit.

**Decision.** Every aggregate inherits `SoftDeletableEntity` (`IsActive` / `IsDeleted` / `IsHidden`) with explicit transitions (`Activate`/`Deactivate`/`Hide`/`Unhide`/`SoftRemove`/`RestoreAll`), each raising a domain event. Reads default to visible rows; seeing inactive/hidden/deleted rows requires passing a `VisibilityScope` flag.

**Consequences.** Nothing is lost by a normal operation; full lifecycle administration and audit are possible. The cost is that every query must respect the flags, and the three flags are independent (an entity can be inactive *and* hidden) — handled by the shared query helpers.

---

## ADR-009 — Multi-tenancy via query filters with cross-tenant authentication

**Status:** Accepted

**Context.** The platform serves multiple tenants, but a caller at `/connect` has not yet proven a tenant — authentication must find the user across tenants, while everything afterward must be tenant-scoped.

**Decision.** Entities carry a nullable `TenantId`. A global EF query filter scopes reads to the current actor's tenant, and writes are stamped with it. Authentication deliberately bypasses the filter (it is cross-tenant); the tenant the caller then acts under is asserted per request.

**Consequences.** Tenant isolation is enforced centrally rather than in each query, and sign-in works before a tenant is known. The cost is the sharp edge of a global filter (you must opt out intentionally for cross-tenant operations). Full details in [tenancy.md](tenancy.md).

---

## Related

- Security implications of these decisions: [security.md](security.md)
- Tenancy model: [tenancy.md](tenancy.md)
- Settings reference: [configuration.md](configuration.md)
- Integration event contracts (ADR-006): [events.md](events.md)
- How-to recipes built on these patterns: [extending.md](extending.md)
