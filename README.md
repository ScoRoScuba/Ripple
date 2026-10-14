# Ripple — Event Ticketing API

A RESTful API for a simplified event ticketing system, built with .NET 9, Dapper, and PostgreSQL.

---

## Getting Started (Docker)

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine with the Compose plugin) and Git. Nothing else — the .NET SDK is not needed to run the API this way.

```bash
git clone https://github.com/ScoRoScuba/Ripple.git
cd Ripple
docker compose up --build -d
```

Then open **Swagger UI at http://localhost:8080/swagger** to explore and call the API.

### What starts

| Service | Port | Role |
|---------|------|------|
| `postgres` | `5432` | PostgreSQL 17. Data is kept in the `postgres_data` volume between runs. |
| `migrations` | — | Applies any pending schema migrations, then exits. Safe to re-run: applied migrations are skipped. |
| `api` | `8080` | The REST API and Swagger UI. Starts only after migrations succeed. |

Startup order is `postgres` (healthy) → `migrations` (completed) → `api`.

> **Port 5432 already in use?** If PostgreSQL is already running on your machine, `postgres` fails to start. Stop the local instance, or map a different host port in an override file (below), e.g. `"5433:5432"`.

### Everyday commands

| Command | What it does |
|---------|--------------|
| `docker compose up --build -d` | Build and start everything in the background |
| `docker compose ps` | Show service status (`migrations` shows *Exited (0)* when it has succeeded) |
| `docker compose logs -f api` | Follow the API's logs (structured Serilog output) |
| `docker compose logs migrations` | See which migrations were applied |
| `docker compose down` | Stop and remove the containers — **data is kept** |
| `docker compose down -v` | Stop and **delete all data** (the `postgres_data` volume) for a clean start |

### Configuring the containers

Containers are configured only through environment variables — local `appsettings.*.json` files are deliberately excluded from images. To add or change settings, create a `docker-compose.override.yml` next to `docker-compose.yml` (Compose picks it up automatically; it is gitignored). For example, to allow a local front-end to call the API and move PostgreSQL to another port:

```yaml
services:
  api:
    environment:
      - Cors__AllowedOrigins__0=http://localhost:3000
  postgres:
    ports: !override
      - "5433:5432"
```

---

## Using the API

The walkthrough below uses `curl` from a Bash-compatible shell (macOS, Linux, Git Bash or WSL); every call can equally be made from Swagger UI. All endpoints live under `/api/v1` — see [API Endpoints](#api-endpoints) for the full list.

```bash
API=http://localhost:8080/api/v1
```

**1. Create an event.** Tier capacities must add up to `totalCapacity`.

```bash
curl -s -X POST $API/events -H "Content-Type: application/json" -d '{
  "name": "Rock Night", "description": "An evening of rock", "venue": "The Arena",
  "date": "2030-06-01", "time": "20:00:00", "totalCapacity": 100,
  "pricingTiers": [
    { "name": "General", "price": 50.00, "capacity": 80 },
    { "name": "VIP",     "price": 120.00, "capacity": 20 }
  ]
}'
```

The `201 Created` response contains the event's `id` and an `id` for each pricing tier. Copy them into variables for the next steps:

```bash
EVENT_ID=<event id>
GENERAL_ID=<id of the General tier>
VIP_ID=<id of the VIP tier>
```

**2. Check availability.**

```bash
curl -s $API/events/$EVENT_ID/availability
```

**3. Buy tickets.** Returns `201 Created` with the ticket details and total price.

```bash
curl -s -X POST $API/events/$EVENT_ID/tickets -H "Content-Type: application/json" \
  -d "{ \"pricingTierId\": \"$VIP_ID\", \"quantity\": 2, \"purchaserName\": \"Jane Doe\", \"purchaserEmail\": \"jane@example.com\" }"
```

**4. Try to buy more than is left.** Returns `409 Conflict` — tickets are never oversold, even under concurrent purchases.

```bash
curl -s -X POST $API/events/$EVENT_ID/tickets -H "Content-Type: application/json" \
  -d "{ \"pricingTierId\": \"$VIP_ID\", \"quantity\": 50, \"purchaserName\": \"Jane Doe\", \"purchaserEmail\": \"jane@example.com\" }"
```

**5. View the sales report** for the event (or for all events at `$API/reports/sales`).

```bash
curl -s $API/reports/sales/$EVENT_ID
```

**6. Update the event.** Send each existing tier with its `id` — that updates the tier in place and keeps its sales. A tier without an `id` is added; an existing tier left out is removed. Here the General price changes, which only affects future purchases:

```bash
curl -s -X PUT $API/events/$EVENT_ID -H "Content-Type: application/json" -d "{
  \"name\": \"Rock Night\", \"description\": \"Doors open at 7pm\", \"venue\": \"The Arena\",
  \"date\": \"2030-06-01\", \"time\": \"20:00:00\", \"totalCapacity\": 100,
  \"pricingTiers\": [
    { \"id\": \"$GENERAL_ID\", \"name\": \"General\", \"price\": 55.00, \"capacity\": 80 },
    { \"id\": \"$VIP_ID\",     \"name\": \"VIP\",     \"price\": 120.00, \"capacity\": 20 }
  ]
}"
```

**7. Sales history is protected.** Removing the VIP tier (which has sales), or deleting the event, returns `409 Conflict`:

```bash
curl -s -X DELETE $API/events/$EVENT_ID
```

Errors are returned as [problem details](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`) with a human-readable `detail`; validation failures return `422` with the failing fields under `errors`.

---

## Running Locally (without Docker for the API)

**Prerequisites:** .NET 9 SDK, and PostgreSQL 17 — either installed locally or just the container: `docker compose up -d postgres`.

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
   It listens on **http://localhost:5084** (Swagger at http://localhost:5084/swagger). Use `--launch-profile https` for https://localhost:7027.

---

## Running Tests

```bash
dotnet test
```

Docker must be running: the database tests start their own PostgreSQL containers via Testcontainers.

| Project | What it covers | Needs |
|---------|----------------|-------|
| `Ripple.UnitTests` | Service logic and business rules, repositories mocked | — |
| `Ripple.ApiTests` | Endpoints, validation, error responses, versioning, CORS and the OpenAPI contract — hosted on a test-only startup that wires just what each scenario needs, with services mocked | — |
| `Ripple.DataStoreTests` | Dapper repositories against a real PostgreSQL: transactions, concurrent purchases, sales-history protection, reporting SQL | **Docker** |
| `Ripple.MigrationTests` | Schema, constraints, foreign keys and indexes after all migrations | **Docker** |

Each database test starts its own PostgreSQL container, so tests are fully independent.

**API contract snapshot.** `OpenApiVerifyTests` compares the generated OpenAPI document with the approved `tests/Ripple.ApiTests/OpenApiTests/*.verified.json`. If you change the API on purpose, the test fails and writes a `.received.json` next to it: review the diff, and if it is what you intended, replace the `.verified.json` with it and commit both together.

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

A single `ExceptionHandlingMiddleware` catches domain exceptions and maps them to HTTP status codes. Every error is returned as problem details (`application/problem+json`); expected outcomes are logged as warnings, anything unmapped as an error with its stack trace.

| Exception | Status | When |
|-----------|--------|------|
| `EventNotFoundException` / `PricingTierNotFoundException` | 404 | Unknown event or pricing tier |
| `InsufficientCapacityException` | 409 | Not enough tickets left in the tier |
| `SalesHistoryConflictException` | 409 | Edit or delete that would remove or invalidate tickets sold |
| `ArgumentException` | 400 | Business-rule violation, e.g. tier capacities not summing to the total |
| `BadHttpRequestException` | 400 | Unreadable body, e.g. `"time": "8pm"`; the detail names the field |
| *(validation failure)* | 422 | Request fails FluentValidation (returned by the endpoint, not the middleware) |
| Anything else | 500 | Unexpected fault. The response carries a generic detail and a `traceId`; the exception itself is only logged, under the same trace id |

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

This solution was built with **Claude Code** (Anthropic). All architectural and design decisions were made by the developer; Claude was used as a pair programmer under that direction.

**Building it.** Claude scaffolded the project structure, generated the initial implementations across all layers, and iterated on build and test failures — including catching a type-mapping issue (`DateOnly`/`TimeOnly` with Dapper + Npgsql) early.

**Hardening it.** In a second phase Claude was used as a reviewer and implementer against the developer's standards, which are captured in [`CLAUDE.md`](CLAUDE.md) (SOLID, pragmatic DRY, independent tests, immutable sales history) so every change is held to them:

- **Review against the requirements** — mapping each ticketing and reporting requirement to its tests exposed gaps (no concurrency test for overselling, untested reporting SQL) and two data-loss defects: repository writes failing with *"Connection already open"*, and event edits cascading into deleted ticket sales. Each was reproduced against the running API before being fixed.
- **Implementing the developer's design choices** — one endpoint per file with `IEndpoint` discovery, URL-segment API versioning, Serilog structured logging, configurable CORS, DbUp migrations, and API tests hosted on a scenario-specific startup rather than the application's `Program.cs`.
- **Proving the tests work** — new tests were checked by deliberately breaking the code they protect (removing the row lock, introducing an off-by-one, reverting the history safeguards) and confirming they fail, then restoring it.
- **Verifying end to end** — behaviour, Docker configuration and this README's commands were exercised against a fresh `docker compose` stack rather than assumed.

Claude also made mistakes along the way — for example a misjudged expected status code and a stale-build test run — which were caught by the same verify-before-claiming discipline.
