# Ripple — Event Ticketing API

A RESTful API for a simplified event ticketing system, built with .NET 9, Dapper, and PostgreSQL.

---

## Quick Start (Docker)

```bash
docker compose up --build
```

The API will be available at **http://localhost:8080** with Swagger UI at **http://localhost:8080/swagger**.

PostgreSQL starts first; the API waits for it to be healthy before binding. The schema is initialised automatically from `scripts/init.sql` on first container run.

---

## Running Locally

**Prerequisites:** .NET 9 SDK, PostgreSQL 17

1. Start PostgreSQL and run `scripts/init.sql` to create the schema.
2. Update `src/Ripple.Api/appsettings.json` with your connection string.
3. Run the API:
   ```bash
   dotnet run --project src/Ripple.Api
   ```

---

## Running Tests

```bash
dotnet test
```

No database required — API tests use a `WebApplicationFactory` with all services mocked.

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/events` | List all events |
| `POST` | `/api/events` | Create an event |
| `GET` | `/api/events/{id}` | Get event by ID |
| `PUT` | `/api/events/{id}` | Replace an event |
| `DELETE` | `/api/events/{id}` | Delete an event |
| `GET` | `/api/events/{id}/availability` | View ticket availability |
| `POST` | `/api/events/{id}/tickets` | Purchase tickets |
| `GET` | `/api/reports/sales` | Sales summary for all events |
| `GET` | `/api/reports/sales/{eventId}` | Sales summary for one event |

---

## Architecture

### Onion Architecture

```
Ripple.Domain          ← Entities, domain exceptions. No dependencies.
Ripple.Application     ← Service interfaces, repository interfaces, DTOs, service logic.
                         Depends on Domain only.
Ripple.Infrastructure  ← Dapper repositories, DbConnectionFactory.
                         Depends on Application (implements its interfaces).
Ripple.Api             ← Minimal API endpoints, validators, middleware.
                         Depends on Application + Infrastructure (wiring only).
```

The key invariant: **Application has zero knowledge of Dapper, Npgsql, or any I/O concern.** Swapping the infrastructure layer (e.g. replacing Dapper with EF Core, or PostgreSQL with SQL Server) requires no changes to Application or Domain.

### Oversell Prevention

Ticket purchasing uses a **PostgreSQL row-level lock** (`SELECT … FOR UPDATE`) inside a `ReadCommitted` transaction. This ensures only one connection can hold the lock for a given pricing tier at a time, so the capacity check and the increment are atomic — even under concurrent load. The DB also enforces `CHECK (tickets_sold <= capacity)` as a last-resort safety net.

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

**Update replaces pricing tiers (DELETE + INSERT)** — When an event is updated, all existing tiers are deleted and replaced. This is simple but loses any `tickets_sold` state on tiers. A production system would instead merge existing tiers (match by ID or name) to preserve sales history. This is noted as a known trade-off.

**Tier capacities must sum to `total_capacity`** — `total_capacity` at the event level is enforced to equal the sum of tier capacities on create/update. This keeps availability calculations consistent. An alternative is treating `total_capacity` as a separate hard cap, but that adds complexity without clear benefit for this model.

**No authentication** — Out of scope for this exercise. In production, purchasing endpoints would require authentication; reporting endpoints would be restricted to organisers.

---

## Further Operationalising

- **Idempotency keys on ticket purchase** — Prevent duplicate purchases from retried requests.
- **Outbox pattern** — Emit domain events (e.g. `TicketPurchased`) reliably for downstream consumers (email confirmation, analytics).
- **Read replicas** — The availability and reporting queries are read-heavy and can be directed to a replica.
- **Connection pooling** — `NpgsqlDataSource` already pools connections; configuring `MaxPoolSize` and `MinPoolSize` for expected concurrency would be a next step.
- **Observability** — Add structured logging with correlation IDs, OpenTelemetry tracing, and a `/health` endpoint for liveness/readiness probes.
- **Soft deletes** — Instead of hard-deleting events, flag them as cancelled so historical ticket data is preserved.

---

## AI Tooling

This solution was built with **Claude Code** (Anthropic). Claude was used to scaffold the project structure, generate the initial implementations across all layers, and iterate on build/test failures. All architectural decisions (Onion layers, locking strategy, validation approach) were authored by the developer; Claude accelerated the writing of repetitive structural code and caught a type-mapping issue (`DateOnly`/`TimeOnly` with Dapper + Npgsql) early in the process.
