# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

Ripple is an event ticketing REST API: .NET 9, Minimal APIs, Dapper, PostgreSQL 17, DbUp migrations.

## Commands

```bash
dotnet build Ripple.slnx
dotnet test Ripple.slnx                      # all tests — Docker must be running
dotnet test tests/Ripple.MigrationTests      # migration tests only (Testcontainers)
docker compose up --build                    # postgres → migrations → api on :8080
```

## Architecture

Onion layering. Dependencies point inwards only:

```
Ripple.Domain       ← entities, domain exceptions. No dependencies.
Ripple.Interfaces   ← service/repository interfaces, DTOs. Depends on Domain.
Ripple.Services     ← business logic. Depends on Interfaces.
Ripple.DataStore    ← Dapper repositories. Depends on Interfaces.
Ripple.Api          ← endpoints, validators, middleware, DI wiring.
Ripple.Migrations   ← DbUp console app; SQL scripts embedded from Scripts/.
```

- `Ripple.Services` must not reference Dapper, Npgsql or any I/O library.
- **One endpoint per file.** Each endpoint is a class implementing `IEndpoint` in `Ripple.Api/Endpoints/<Feature>/`, named `<Action>Endpoint` (e.g. `CreateEventEndpoint`), with its route mapping in `Map` and its handler in a private static `Handle` method. Endpoints are discovered and registered automatically; don't map routes in `Program.cs`.
- **API versioning** is URL-segment (`/api/v1/...`). `Map` receives the versioned `/api/v{version}` group: map routes relative to it (`"events/{id:guid}"`, never `"/api/..."`) and always declare the version with `.MapToApiVersion(ApiVersions.V1)`. Supported versions live only in `Ripple.Api/Versioning/ApiVersions.cs`. Never change a released version's contract; add a new version instead.
- Input validation lives in FluentValidation validators in `Ripple.Api`, not in services.
- Domain exceptions map to HTTP status codes in `ExceptionHandlingMiddleware`; add new mappings there.

## Design principles

These are important in this codebase. Follow them in new code and when changing existing code, and call out any trade-off where one is bent.

**SOLID**

- **Single Responsibility (SRP)** — each class has one reason to change. Endpoints handle HTTP only, validators validate, services hold business rules, repositories do data access. If a class or method needs "and" to describe it, split it. (e.g. `MigrationRunner` runs migrations; `Program.cs` only reads config and reports the result.)
- **Open/Closed (OCP)** — extend behaviour by adding new types (a new validator, a new exception mapping, a new migration script) rather than editing working code paths with extra branches.
- **Liskov Substitution (LSP)** — any implementation of an interface must honour its full contract: same nullability, same exceptions for the same conditions, no `NotImplementedException`. Mocks in tests must behave like the real thing.
- **Interface Segregation (ISP)** — keep interfaces small and focused on what the caller needs. Don't add a method to an existing interface just because it's convenient; consider a new interface.
- **Dependency Inversion (DIP)** — depend on abstractions in `Ripple.Interfaces`, injected via the constructor. Never `new` up a service or repository inside another one; wire everything in `Ripple.Api` DI.

**Also**

- **DRY, applied pragmatically** — remove duplication of *knowledge* (a business rule, a config chain that must stay in sync) so it changes in one place. Don't extract shared code just because a few lines look alike, and don't add an abstraction that makes the code harder to read than the duplication it removes. A little repetition is fine — especially in tests, where each test should read on its own.
- **KISS / YAGNI** — the simplest thing that meets the current requirement. No speculative abstractions, options or extension points.
- Small methods with intention-revealing names; prefer guard clauses over deep nesting.
- Composition over inheritance.

## Configuration

- `src/Ripple.Api/appsettings.json` holds shared, non-secret defaults and is the only settings file committed under `src/`.
- Environment files (`appsettings.Development.json`, etc.) are local-only and gitignored. Never add one to the repo.
- `docs/appsettings.Example.json` is the committed template: copy it to `src/Ripple.Api/appsettings.Development.json` and fill in local values. When a new setting is added, add it to the example too. Guides and the README should link to it rather than repeat its contents.

## Logging (Serilog)

- Serilog is configured once, in `Ripple.Api/Logging/SerilogExtensions.cs`. Levels and sinks live in the `Serilog` section of `appsettings*.json`; enrichers are set in code.
- Classes take `ILogger<T>` (Microsoft.Extensions.Logging) by constructor injection. Only the host projects (`Ripple.Api`, `Ripple.Migrations`) reference Serilog; never use the static `Log` outside `Program.cs`.
- Message format: `"[ClassName]:[MethodName] - Description {StructuredProperty}"`. Use named placeholders, never string interpolation.
- Log state changes (created, updated, deleted, purchased) at `Information` in services. Don't log-and-rethrow: exceptions are logged once, by `ExceptionHandlingMiddleware` (`Warning` for mapped domain exceptions, `Error` for unhandled).
- Never log personal data (purchaser name/email) or connection strings.

## Database & migrations

- The schema source of truth is `src/Ripple.Migrations/Scripts/`. Don't add standalone schema SQL elsewhere.
- Migration scripts are append-only and numbered `NNNN_Description.sql`. Never modify a script that has already been committed; add a new one.
- Keep SQL explicit in the Dapper repositories (no query builders). Concurrency-sensitive writes use `SELECT … FOR UPDATE` inside a transaction.

## Testing guidelines

- **Tests must be independent.** Each test sets up its own state and its own Postgres container; do not introduce shared fixtures (`IClassFixture`/`ICollectionFixture`) to save time.
- **Test our code, not the libraries.** Don't assert on behaviour that belongs to DbUp, Dapper, Npgsql or Postgres itself; assert on the outcomes our code is responsible for.
- Don't hard-code values that are derived from the codebase (e.g. the number of migration scripts); compute them.
- **Tests never boot the API's `Program.cs`.** API tests host `ScenarioStartup` (in `tests/Ripple.ApiTests/Scaffolding/`) via `CustomWebApplicationFactory<TStartup>`, wiring only what they exercise — endpoints, validators, exception middleware and mocked services. Register shared wiring through the same extension methods `Program.cs` uses (`AddEndpoints`, `AddValidators`, …) rather than copying it; when you add new wiring to `Program.cs`, put it in an extension method so tests can opt in.
- Get mocks with `factory.Services.ResetMock<T>()` in the test class constructor so no setup leaks between tests.
- `Ripple.UnitTests` and `Ripple.ApiTests` use Moq and need no database. `Ripple.DataStoreTests` and `Ripple.MigrationTests` run against a real Postgres via Testcontainers. Repository changes need a `Ripple.DataStoreTests` test: mocked unit tests can't catch SQL or connection-handling bugs.
- Run the full test suite and confirm it passes before saying a change is done.

## Workflow

- Work is committed directly to `master`.
- Don't commit or push unless asked.
