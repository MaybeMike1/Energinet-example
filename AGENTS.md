# AGENTS.md — GridFlow

> Instruction file for AI coding agents (Cursor, etc.). Read this fully before writing code.
> This is a **portfolio project**. Code quality, tests, and operational maturity matter more than feature count.

## 1. What we are building

**GridFlow** is a small but production-shaped **.NET + Azure data & integration service** that:

1. **Ingests** Danish energy market data from Energinet's public **Energi Data Service** API on a schedule (background worker).
2. **Normalizes and stores** it in **SQL Server** via EF Core with idempotent upserts.
3. **Exposes** it through a versioned **REST API** (BFF-style, tailored to the UI).
4. **Visualizes** it in a **Blazor** dashboard (charts + filterable table).
5. Is **observable and operable**: structured logging, health checks, resilient HTTP, CI/CD.

### Why it is built this way
The goal is to demonstrate the exact skills .NET hiring managers screen for: integration across a system landscape, clean data modelling, both *drift* (operations) and *development*, a real test pyramid, and CI/CD. Prefer realistic, boring, correct engineering over clever shortcuts. Every milestone should end in a state that builds, tests green, and runs.

## 2. Tech stack (pin these)

- **.NET 10 (LTS)**, C# latest, `Nullable` enabled, `ImplicitUsings` enabled, `TreatWarningsAsErrors` true.
- **ASP.NET Core** Web API (Minimal APIs) + **Blazor** (interactive server render mode) for the dashboard.
- **EF Core 10** with **SQL Server** (LocalDB / container for dev).
- **Worker Service** (`BackgroundService`) for ingestion.
- **Serilog** for structured logging; **Microsoft.Extensions.Diagnostics.HealthChecks**.
- **Microsoft.Extensions.Http.Resilience** (or Polly) for retries/timeouts/circuit breaker on the external API.
- **xUnit** + **FluentAssertions** + **NSubstitute** (unit), **Testcontainers** for SQL Server (integration), `WebApplicationFactory` (API tests).
- **CircleCI** for CI.

Do **not** add other frameworks (MediatR, AutoMapper, etc.) unless a milestone calls for it. Keep dependencies lean and justified.

## 3. Solution architecture

Single solution, clear layering. Dependencies point inward (Domain has no outward references).

```
GridFlow.sln
 ├─ src/
 │   ├─ GridFlow.Domain/          # entities, value objects, domain rules. No EF/HTTP deps.
 │   ├─ GridFlow.Application/     # use cases, DTOs, interfaces (ports). Depends on Domain only.
 │   ├─ GridFlow.Infrastructure/  # EF Core, DbContext, the Energi Data Service HTTP client, migrations.
 │   ├─ GridFlow.Api/             # Minimal API host + BFF endpoints + health checks + Serilog wiring.
 │   ├─ GridFlow.Worker/          # BackgroundService that runs ingestion on a timer.
 │   └─ GridFlow.Web/             # Blazor dashboard (consumes GridFlow.Api).
 └─ tests/
     ├─ GridFlow.UnitTests/
     ├─ GridFlow.IntegrationTests/   # Testcontainers SQL Server
     └─ GridFlow.ApiTests/           # WebApplicationFactory end-to-end
```

Keep the Api and Worker as **separate hosts** but sharing Application/Infrastructure. They can be deployed independently (good story for Azure: API as App Service/Container App, Worker as a Container App job or WebJob).

## 4. External domain: Energi Data Service

Public, free, **no API key**. Source attribution required: "Source: Energinet (www.energidataservice.dk)".

- **Base URL:** `https://api.energidataservice.dk/dataset/{DatasetName}`
- **Default dataset:** `Gasflow` (Danish gas flow — on-domain for the gas market). Make the dataset **configurable** so it can be swapped (e.g. `CO2Emis`, `DayAheadPrices`, `ProductionConsumptionSettlement`).
- **Do NOT use `Elspotprices`** — it was discontinued after 2025-09-30. Use `DayAheadPrices` if you need price data.
- **Query params:** `start`, `end`, `filter` (JSON), `limit`, `offset`, `sort`, `columns`.
  - Supports **dynamic timestamps**, e.g. `?start=now-PT15M` (last 15 minutes). Use these instead of hard-coded clocks.
- **Response shape** (model this):
  ```json
  { "total": 123, "dataset": "Gasflow", "records": [ { /* fields vary by dataset */ } ] }
  ```
  The agent must **inspect the live response** for the chosen dataset and model only the fields used. Do not invent columns — fetch a sample first and map from reality.

### Rate limiting (important — this is a "drift" signal)
- The API applies per-dataset rate limits and returns **HTTP 429** with a "try again in X seconds" message when exceeded.
- **Rule of thumb:** one request per the dataset's update frequency, using a dynamic timestamp window. Do not poll continuously.
- The ingestion client must: respect 429 (read the retry hint, back off), use bounded retries with jittered exponential backoff, set a sane HTTP timeout, and never hammer the API in a tight loop.

## 5. Coding conventions

- Async all the way; suffix async methods `Async`; pass `CancellationToken` through every I/O path.
- One public type per file; file-scoped namespaces; `var` when the type is obvious.
- Domain entities have behavior and invariants, not just public setters. Use private setters + factory/constructors.
- Options pattern (`IOptions<T>`) for all config; validate options at startup with `ValidateOnStart()`.
- No `DateTime.Now`/`UtcNow` directly in logic — inject `TimeProvider` so time is testable.
- Return `Results.Problem(...)` / RFC 7807 ProblemDetails for API errors; never leak stack traces.
- Conventional commits (`feat:`, `fix:`, `test:`, `chore:`). Keep commits small and green.

## 6. Data & persistence

- EF Core code-first. One `DbContext` (`GridFlowDbContext`) in Infrastructure.
- Each ingested record maps to an entity with a **natural key** (e.g. dataset + timestamp + zone). Define a unique index on it.
- Ingestion must be **idempotent**: upsert on the natural key so re-running the same window never duplicates rows. Prefer a bulk merge or `ON CONFLICT`-style upsert; a "fetch existing keys → insert missing" pass is acceptable to start.
- Use **migrations** (`dotnet ef migrations add`). Never edit the DB by hand. Apply migrations at startup only in Development; in other environments apply them explicitly.
- Store timestamps as UTC.

## 7. Ingestion worker

- A `BackgroundService` with a `PeriodicTimer` whose interval comes from config (default aligned to the dataset's update frequency).
- Each tick: compute a dynamic window (`start=now-PTxx`), call the typed HTTP client, map records, upsert, log a summary (records fetched / inserted / skipped, duration).
- Wrap each tick in try/catch so one bad cycle never kills the service; log and continue.
- Expose ingestion health (last successful run timestamp, last error) via a health check.

## 8. API layer (BFF)

- Minimal APIs grouped by resource, URL-versioned (`/api/v1/...`).
- Endpoints are **tailored to the dashboard's needs** (BFF), not a generic CRUD dump. Examples:
  - `GET /api/v1/flows?from=&to=&zone=&page=&pageSize=` → paginated, filtered series.
  - `GET /api/v1/flows/summary?from=&to=` → aggregates for the chart.
  - `GET /health` (liveness) and `GET /health/ready` (readiness, includes DB + ingestion freshness).
- Always paginate list endpoints; cap `pageSize`. Validate query params; return 400 ProblemDetails on bad input.
- Generate OpenAPI; expose Swagger UI in Development only.

## 9. Frontend (Blazor) — required

Energinet explicitly asks for **Blazor**, so it must be visible and real.
- Blazor (interactive server) dashboard with: a date-range filter, a zone filter, a time-series **chart**, and a paginated data **table**.
- Talk to the API through a typed `HttpClient` (the BFF), never to the DB or the external API directly.
- Keep it clean and readable; no heavy component libraries needed. Loading and empty/error states must be handled.

## 10. Observability

- **Serilog** with structured logging; enrich with a correlation/request id; write JSON in non-Development.
- **Health checks** on both hosts; readiness includes SQL connectivity and ingestion freshness.
- Optional stretch: **OpenTelemetry** traces/metrics exported to the console (and Azure Monitor when deployed).

## 11. Configuration & secrets

- **Never commit secrets.** Connection strings and any keys go in **user-secrets** (dev) and environment variables / Azure App Configuration + Key Vault (deployed).
- `appsettings.json` holds only non-secret defaults; `appsettings.Development.json` for local non-secret overrides.
- All settings bound via the options pattern with startup validation.

## 12. Testing (the pyramid is the point)

- **Unit (most):** domain rules, mapping, the ingestion windowing logic, options validation. Fast, no I/O. Use `TimeProvider` fakes.
- **Integration (some):** Infrastructure against a **real SQL Server via Testcontainers**; verify migrations apply and upserts are idempotent (insert the same window twice → row count unchanged).
- **API (few):** `WebApplicationFactory` hitting real endpoints with the external API **stubbed** (a fake `HttpMessageHandler` returning canned JSON). Assert status codes, ProblemDetails, pagination.
- Never call the live Energi Data Service API from tests — stub it.
- Target meaningful coverage of logic, not a percentage. Every bug fix gets a regression test.
- **Naming convention:** name every test method `Given<State>_When<Action>_Then<ExpectedResult>` (e.g. `GivenBlankDataset_WhenCreating_ThenThrowsArgumentException`). One behaviour per test, structured Arrange/Act/Assert.

## 13. CI/CD (CircleCI)

Config lives in **`.circleci/config.yml`**. On every push + PR:
1. `dotnet restore` (with NuGet cache).
2. `dotnet format --verify-no-changes` (fail on style drift).
3. `dotnet build -c Release` (warnings are errors).
4. `dotnet test -c Release` (unit + integration), publishing JUnit results to CircleCI.

**Executor choice matters because of Testcontainers.** The integration tests start a real SQL Server container and connect to it over `localhost`. This works reliably only on the CircleCI **`machine` executor** (a full Linux VM with a local Docker daemon) — **not** the `docker` executor, where `setup_remote_docker` runs containers on a separate host and their mapped ports are not reachable from the test process. So run the job (or at least the integration tests) on `machine: ubuntu-2404:current`.

Install the SDK with the official `dotnet-install.sh` pinned to `--channel 10.0` for a reproducible build (more robust than a community orb for a brand-new SDK). Add the **`JunitXml.TestLogger`** package to the test projects and pass `--logger "junit"` so CircleCI's `store_test_results` shows test reports.

Optional optimization: split into two jobs — a fast `lint-build-unit` job on the `docker` executor (`mcr.microsoft.com/dotnet/sdk:10.0`) for format/build/unit tests, and a separate `integration` job on the `machine` executor for the Testcontainers tests. Start with the single-job config below; split later if CI time becomes annoying.

Stretch: a `deploy` job that builds a container image and deploys to Azure Container Apps, gated to the `main` branch via a workflow filter and a CircleCI context holding the Azure credentials (never hard-code secrets).

## 14. Commands

```bash
# build / run / test
dotnet build
dotnet run --project src/GridFlow.Api
dotnet run --project src/GridFlow.Worker
dotnet run --project src/GridFlow.Web
dotnet test

# EF Core (run from repo root; -p Infrastructure, -s the host that owns config)
dotnet ef migrations add <Name> -p src/GridFlow.Infrastructure -s src/GridFlow.Api
dotnet ef database update          -p src/GridFlow.Infrastructure -s src/GridFlow.Api

# secrets (per host project)
dotnet user-secrets init   --project src/GridFlow.Api
dotnet user-secrets set "ConnectionStrings:GridFlow" "Server=...;Database=GridFlow;..." --project src/GridFlow.Api

# formatting
dotnet format
```

## 15. Definition of Done (per milestone)

- Solution builds in Release with zero warnings.
- All tests green; new logic has tests.
- `dotnet format` clean.
- No secrets in the repo or git history.
- README updated if commands or setup changed.
- The thing actually runs end-to-end for what the milestone covers.

## 16. Build roadmap (work milestone by milestone — do not skip ahead)

1. **Scaffold:** solution + projects + folders per §3, `.editorconfig`, `Directory.Build.props` (nullable, warnings-as-errors, central package versions), README, `.gitignore`. CircleCI config (`.circleci/config.yml`) that builds + (empty) tests.
2. **Domain + persistence:** entities, `GridFlowDbContext`, first migration, integration test proving migration applies (Testcontainers).
3. **External client:** typed `HttpClient` for Energi Data Service with resilience + 429 handling. **Fetch one live sample of `Gasflow`** and model the records from the real response. Unit-test mapping with canned JSON.
4. **Ingestion worker:** `BackgroundService` + dynamic window + idempotent upsert. Integration test: ingest same window twice → no duplicates. Add ingestion-freshness health check.
5. **API (BFF):** versioned endpoints (§8), pagination, ProblemDetails, OpenAPI, health endpoints. API tests via `WebApplicationFactory` with the external API stubbed.
6. **Blazor dashboard:** filters + chart + table against the BFF, with loading/error/empty states.
7. **Observability + polish:** Serilog JSON, correlation id, readiness checks, README with architecture diagram and screenshots.
8. **Stretch (optional):** containerize, Azure deploy (Container Apps), OpenTelemetry, CircleCI deploy job. Optional light AI feature (natural-language query or auto-generated summary of a series) — keep it additive, not core.

## 17. Guardrails — do NOT

- Do **not** commit connection strings, keys, or any secret. Use user-secrets / env vars.
- Do **not** poll the external API faster than its update frequency; always handle 429.
- Do **not** invent dataset columns — fetch a real sample and map from it.
- Do **not** let the Blazor app or Worker touch the DB or external API directly except through the intended layer (Web → API; Worker/API → Infrastructure).
- Do **not** introduce new dependencies or large refactors without it being in the current milestone. Ask / note it instead.
- Do **not** weaken tests or disable warnings to make CI pass. Fix the cause.
- Keep PRs/commits small and each one green.

## 18. PFA variant (optional, same codebase)

The architecture is data-source-agnostic. To produce a finance-flavoured twin for PFA, swap the ingestion client's source (e.g. Nationalbankens exchange-rate API or a pension-projection calculation) behind the same `IMarketDataSource` port — everything else (persistence, BFF, Blazor, tests, CI) stays. Do this only as a separate, later track; GridFlow's default domain is Energinet gas/electricity data.
```