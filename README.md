# Ripple — Event Ticketing API

A RESTful API for a simplified event ticketing system, built with .NET 9, Dapper, and PostgreSQL.

---

## Quick Start (Docker)

```bash
docker compose up --build
```

The API will be available at **http://localhost:8080** with Swagger UI at **http://localhost:8080/swagger**.

Startup order: PostgreSQL starts and becomes healthy → the `migrations` service applies any pending schema migrations and exits → the API starts. Re-running is safe; already-applied migrations are skipped.

---

## Running Locally

**Prerequisites:** .NET 9 SDK, PostgreSQL 17 (or `docker compose up postgres`)

1. Copy [`docs/appsettings.Example.json`](docs/appsettings.Example.json) to `src/Ripple.Api/appsettings.Development.json` and fill in your local values. This file is gitignored — never commit it.
2. Create or upgrade the schema:
   ```bash
   dotnet run --project src/Ripple.Migrations -- "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=<your-password>"
   ```
   The database is created if it doesn't exist. (If `ConnectionStrings__DefaultConnection` is set in your environment, it takes precedence over the argument.)
3. Run the API:
   ```bash
   dotnet run --project src/Ripple.Api
   ```

---

## Running Tests

```bash
dotnet test
```

| Project | What it covers | Needs |
|---------|----------------|-------|
| `Ripple.UnitTests` | Service logic, repositories mocked | — |
| `Ripple.ApiTests` | Endpoints via `WebApplicationFactory`, services mocked | — |
| `Ripple.DataStoreTests` | Dapper repositories (transactions, row locking, capacity checks) against a real PostgreSQL | **Docker running** (Testcontainers) |
| `Ripple.MigrationTests` | Schema, constraints and indexes against a real PostgreSQL | **Docker running** (Testcontainers) |

Each database test starts its own PostgreSQL container, so tests are fully independent.

---

## API Endpoints

All endpoints are versioned in the URL (`/api/v{version}/...`); the current version is **v1**. Every response carries an `api-supported-versions` header, and an unsupported version returns `404`. See [API Versioning](#api-versioning).

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/events` | List all events |
| `POST` | `/api/v1/events` | Create an event |
| `GET` | `/api/v1/events/{id}` | Get event by ID |
| `PUT` | `/api/v1/events/{id}` | Replace an event |
| `DELETE` | `/api/v1/events/{id}` | Delete an event |
| `GET` | `/api/v1/events/{id}/availability` | View ticket availability |
| `POST` | `/api/v1/events/{id}/tickets` | Purchase tickets |
| `GET` | `/api/v1/reports/sales` | Sales summary for all events |
| `GET` | `/api/v1/reports/sales/{eventId}` | Sales summary for one event |

---

## Architecture

### Onion Architecture

```
Ripple.Domain       ← Entities, domain exceptions. No dependencies.
Ripple.Interfaces   ← Service and repository interfaces, DTOs. Depends on Domain.
Ripple.Services     ← Business logic. Depends on Interfaces.
Ripple.DataStore    ← Dapper repositories, DbConnectionFactory. Depends on Interfaces.
Ripple.Api          ← Minimal API endpoints, validators, middleware, logging, DI wiring.
Ripple.Migrations   ← DbUp console app; versioned SQL scripts embedded from Scripts/.
```

The key invariant: **Services have zero knowledge of Dapper, Npgsql, or any I/O concern.** Swapping the data layer (e.g. replacing Dapper with EF Core, or PostgreSQL with SQL Server) requires no changes to Services or Domain.

### Database Migrations

The schema is managed by **DbUp** in `Ripple.Migrations`. Scripts live in `src/Ripple.Migrations/Scripts/`, are named `NNNN_Description.sql`, run in order, and are recorded in the `schemaversions` table so each runs exactly once. Scripts are append-only: to change the schema, add a new script rather than editing one that has been committed.

### Logging

Structured logging uses **Serilog**. Levels and sinks are configured in the `Serilog` section of `appsettings.json` (console by default; the [example settings](docs/appsettings.Example.json) add a daily rolling file under `logs/`). Every entry is enriched with machine name, environment, thread id, client IP and a correlation id — send an `x-correlation-id` header to trace a request end to end, or one is generated.

- One summary line per HTTP request (method, path, status, duration).
- Handled domain exceptions (404/409/400) log as `Warning`; unhandled exceptions log as `Error` with the stack trace — each failure is logged exactly once.
- State changes (event created/updated/deleted, tickets purchased) log at `Information`. Purchaser personal data is never logged.

### Oversell Prevention

Ticket purchasing uses a **PostgreSQL row-level lock** (`SELECT … FOR UPDATE`) inside a `ReadCommitted` transaction. This ensures only one connection can hold the lock for a given pricing tier at a time, so the capacity check and the increment are atomic — even under concurrent load. The DB also enforces `CHECK (tickets_sold <= capacity)` as a last-resort safety net.

### API Versioning

Versioning uses **Asp.Versioning** with the version in the URL path. Supported versions are declared once in `ApiVersions`; `MapEndpoints` maps every endpoint under a single `/api/v{version}` route group, and each endpoint declares the version(s) it implements with `MapToApiVersion`. Adding v2 means adding it to `ApiVersions` and mapping the endpoints that change; unchanged v1 endpoints keep working. Swagger publishes one document per version (`/swagger/v1/swagger.json`).

### CORS

Browser clients on other origins are allowed only if listed in `Cors:AllowedOrigins` (any method and header). The committed default is an empty list, so **no cross-origin access until configured** — add your front-end's origin in local settings (see the [example](docs/appsettings.Example.json)) or via environment variables, e.g. `Cors__AllowedOrigins__0=https://app.example.com`. The `Location` and `api-supported-versions` response headers are exposed so browser code can read them.

### Validation

Requests are validated with **FluentValidation** before reaching the service layer. Invalid requests return `422 Unprocessable Entity` with a structured `ValidationProblemDetails` body. The validator is injected directly into each endpoint, keeping the service layer clean of input-validation concerns.

### Error Handling

A single `ExceptionHandlingMiddleware` catches domain exceptions and maps them to appropriate HTTP status codes:

| Exception | Status |
|-----------|--------|
| `EventNotFoundException` / `PricingTierNotFoundException` | 404 |
| `InsufficientCapacityException` | 409 |
| `ArgumentException` | 400 |
| Anything else | 500 |

---

## Design Decisions & Trade-offs

**Dapper over EF Core** — Dapper keeps SQL explicit and readable, which is valuable in a domain where query performance and concurrency control (e.g. `FOR UPDATE`) are important. The trade-off is more boilerplate for CRUD operations and manual mapping setup (`MatchNamesWithUnderscores`, `DateOnly`/`TimeOnly` type handlers).

**Sales history is immutable** — A ticket purchase is a permanent record of what was bought and the price paid. Editing an event updates its pricing tiers *in place*: each tier in the request carries its `id` (tiers without one are added), so sold counts carry over. Edits that would invalidate history are refused with `409 Conflict`: removing a tier that has sales, reducing a tier's capacity below what has been sold, or deleting an event that has sales. Changing a tier's price is allowed and only affects future purchases. The database enforces the same rules (`ON DELETE RESTRICT` from tickets, `CHECK (tickets_sold <= capacity)`), so a purchase landing mid-edit cannot slip through. Sales reports are computed from the ticket records — revenue is quantity × price *paid* — so a later price change never rewrites past revenue. *(An earlier version replaced tiers on every edit, which cascaded and deleted the tickets sold — see migration `0002`.)*

**Tier capacities must sum to `total_capacity`** — `total_capacity` at the event level is enforced to equal the sum of tier capacities on create/update. This keeps availability calculations consistent. An alternative is treating `total_capacity` as a separate hard cap, but that adds complexity without clear benefit for this model.

**No authentication** — Out of scope for this exercise. In production, purchasing endpoints would require authentication; reporting endpoints would be restricted to organisers.

---

## Further Operationalising

- **Idempotency keys on ticket purchase** — Prevent duplicate purchases from retried requests.
- **Outbox pattern** — Emit domain events (e.g. `TicketPurchased`) reliably for downstream consumers (email confirmation, analytics).
- **Read replicas** — The availability and reporting queries are read-heavy and can be directed to a replica.
- **Connection pooling** — `NpgsqlDataSource` already pools connections; configuring `MaxPoolSize` and `MinPoolSize` for expected concurrency would be a next step.
- **Observability** — Structured logging with correlation IDs is in place (Serilog); next steps are OpenTelemetry tracing/metrics and a `/health` endpoint for liveness/readiness probes.
- **Event cancellation** — Events with sales can't be deleted (their history is protected); a `cancelled` status would let organisers withdraw such events, with refunds recorded as new records rather than by changing or deleting purchases.

---

## AI Tooling

This solution was built with **Claude Code** (Anthropic). Claude was used to scaffold the project structure, generate the initial implementations across all layers, and iterate on build/test failures. All architectural decisions (Onion layers, locking strategy, validation approach) were authored by the developer; Claude accelerated the writing of repetitive structural code and caught a type-mapping issue (`DateOnly`/`TimeOnly` with Dapper + Npgsql) early in the process.
