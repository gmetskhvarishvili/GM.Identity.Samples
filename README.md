<p align="center">
  <img src="icon.png" alt="GM.Identity Samples" width="140" height="140" />
</p>

# GM.Identity Samples

[![CI](https://github.com/gmetskhvarishvili/GM.Identity.Samples/actions/workflows/ci.yml/badge.svg)](https://github.com/gmetskhvarishvili/GM.Identity.Samples/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A layered **DDD + CQRS** identity and access-management sample built on the **GM.\*** ecosystem.
It shows how to assemble a real identity server and a token-validating API gateway from
**[GM.Identity](https://www.nuget.org/packages/GM.Identity)** (password hashing, tokens, claims),
**[GM.API](https://www.nuget.org/packages/GM.API)** (Web API startup, versioning, Swagger),
**[GM.Mediator](https://www.nuget.org/packages/GM.Mediator)** (CQRS dispatch),
**[GM.EntityFramework](https://www.nuget.org/packages/GM.EntityFramework)** (repository / unit of
work / auditing), **[GM.Messaging](https://www.nuget.org/packages/GM.Messaging)** (Wolverine +
RabbitMQ outbox), **[GM.HttpClient](https://www.nuget.org/packages/GM.HttpClient)** (Refit + Polly)
and **[GM.Exceptions](https://www.nuget.org/packages/GM.Exceptions)** (localized error handling).
Targets **.NET 10**, backed by **PostgreSQL**.

## What it demonstrates

- An **identity server** (`GM.Identity.Sample.API`) exposing users, roles, permissions, scopes,
  operations, clients and accounts, with `AddGMIdentity()` wiring PBKDF2 hashing / token / claims
  helpers, EF Core migrations + seeding, and external-login scaffolding (Google / Facebook).
- An **API gateway** (`GM.Identity.Sample.Gateway.API`) that validates tokens via **OpenIddict**
  introspection against the identity server, enforces **permission-based** authorization policies,
  and calls the identity API over a typed **GM.HttpClient**.
- **DDD bounded contexts** (Identity, AccessControl, Authorization, Messaging) modelling the
  domain on top of `GM.Identity.Domain`'s entities and `GM.EntityFramework.Domain`'s building
  blocks.
- **CQRS** with GM.Mediator — controllers just `Mediator.Send(...)`; commands and queries live in
  the Application layer.
- **Messaging** — a Wolverine-based producer worker and outbox persistence via GM.Messaging.

## Architecture

```
GM.Identity.Sample.Domain/           # DDD aggregates & bounded contexts (Identity/AccessControl/…)
GM.Identity.Sample.Application/       # CQRS commands + queries (GM.Mediator handlers)
GM.Identity.Sample.Common/            # shared resources & localized exceptions (GM.Exceptions)
GM.Identity.Sample.Infrastructure/    # HTTP clients, JWT, messaging integration
GM.Identity.Sample.Persistence/       # ApplicationDbContext, EF configs, migrations, seeding
GM.Identity.Sample.API/               # identity server: controllers + composition root
GM.Identity.Sample.Gateway.API/       # token-validating API gateway (OpenIddict + permissions)
GM.Identity.Sample.Producer.Worker/   # Wolverine messaging producer worker
tests/GM.Identity.Sample.Tests/       # xUnit tests
```

Dependencies flow inward: Domain has no infrastructure dependencies; Application depends on Domain;
Persistence and Infrastructure implement outward concerns; the APIs are the composition roots.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **PostgreSQL**. A quick local instance:
  ```bash
  docker run --name gm-identity-db -e POSTGRES_PASSWORD=123456 -p 5432:5432 -d postgres
  ```

## Running

1. Set the connection string in
   [`GM.Identity.Sample.API/appsettings.json`](GM.Identity.Sample.API/appsettings.json)
   under `ConnectionStrings:ApplicationDatabase`. Replace the sample OAuth / OpenIddict
   credentials with your own (see the note below).
2. Run the identity server — it applies pending EF Core migrations and seeds on startup:
   ```bash
   dotnet run --project GM.Identity.Sample.API
   ```
3. (Optional) Run the gateway, which validates tokens against the identity server:
   ```bash
   dotnet run --project GM.Identity.Sample.Gateway.API
   ```
   Then open each service's Swagger UI to try the endpoints.

### Identity API endpoints

Controllers: `Accounts`, `Users`, `Roles`, `Permissions`, `Scopes`, `Operations`, `Clients` —
covering authentication/registration and RBAC administration. The gateway exposes a
`Permissions` controller behind OpenIddict validation.

> **Note on credentials:** `appsettings.json` and the gateway's `Program.cs` ship with
> **placeholder** OAuth / OpenIddict client IDs and secrets so the sample is self-explanatory.
> Treat them as examples only — replace them with your own values (ideally via user secrets or
> environment variables) and never reuse them in a real deployment.

## Testing

```bash
dotnet test
```

## License

MIT — see [LICENSE](LICENSE).
