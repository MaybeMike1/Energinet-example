# GridFlow

A small, production-shaped **.NET 10 + Azure-ready data & integration service** that ingests Danish
energy-market data from Energinet's public [Energi Data Service](https://www.energidataservice.dk/)
API, normalizes and stores it in SQL Server via EF Core, exposes it through a versioned BFF-style
REST API, and visualizes it in a Blazor dashboard.

This is a portfolio project: code quality, tests, and operational maturity are the point.

> Source: Energinet (www.energidataservice.dk)

## Architecture

Single solution, clean layering. Dependencies point inward (Domain has no outward references).

```
GridFlow.sln
 ├─ src/
 │   ├─ GridFlow.Domain/          # entities, value objects, domain rules. No EF/HTTP deps.
 │   ├─ GridFlow.Application/     # use cases, DTOs, interfaces (ports). Depends on Domain only.
 │   ├─ GridFlow.Infrastructure/  # EF Core, DbContext, Energi Data Service HTTP client, migrations.
 │   ├─ GridFlow.Api/             # Minimal API host + BFF endpoints + health checks + Serilog wiring.
 │   ├─ GridFlow.Worker/          # BackgroundService that runs ingestion on a timer.
 │   └─ GridFlow.Web/             # Blazor dashboard (consumes GridFlow.Api).
 └─ tests/
     ├─ GridFlow.UnitTests/
     ├─ GridFlow.IntegrationTests/   # Testcontainers SQL Server
     └─ GridFlow.ApiTests/           # WebApplicationFactory end-to-end
```

The API and Worker are separate hosts that share Application/Infrastructure, so they can be deployed
independently (API as an App Service / Container App, Worker as a Container App job or WebJob).

## Tech stack

- .NET 10 (LTS), C# latest, nullable + implicit usings, warnings-as-errors.
- ASP.NET Core Minimal APIs + Blazor (interactive server) for the dashboard.
- EF Core 10 with SQL Server (LocalDB / container for dev).
- Worker Service (`BackgroundService`) for ingestion.
- Serilog for structured logging; `Microsoft.Extensions.Diagnostics.HealthChecks`.
- `Microsoft.Extensions.Http.Resilience` for retries/timeouts/circuit breaker on the external API.
- xUnit + FluentAssertions + NSubstitute (unit), Testcontainers for SQL Server (integration),
  `WebApplicationFactory` (API tests).
- CircleCI for CI.

Package versions are managed centrally in [`Directory.Packages.props`](Directory.Packages.props);
shared build settings live in [`Directory.Build.props`](Directory.Build.props).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for local `dotnet run` / tests)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (recommended — runs SQL Server and the full stack via Compose)
- SQL Server LocalDB (optional — only if you run hosts outside Docker without Compose)

## Run with Docker Compose (recommended)

No local SQL Server install required. From the repo root:

```bash
cp .env.example .env          # edit MSSQL_SA_PASSWORD if you like
docker compose up --build -d
```

| Service | URL |
|---------|-----|
| Dashboard (Blazor) | http://localhost:5125 |
| API (BFF) | http://localhost:5087 |
| API health | http://localhost:5087/health/ready |
| SQL Server | `localhost,1433` (sa / password from `.env`) |

The stack starts **SQL Server**, applies EF migrations on API startup, then runs the **Worker** (ingests Gasflow every 5 minutes by default in Compose) and **Web** (calls the API at `http://api:8080` inside the network).

Useful commands:

```bash
docker compose logs -f worker    # watch ingestion
docker compose ps
docker compose down              # stop containers
docker compose down -v           # stop and delete the SQL volume
```

The dashboard auto-refreshes on a timer (default 1 minute, configurable via `Dashboard:RefreshInterval`) and reloads immediately when you change the date range or zone — no Apply button needed.

Override the worker interval in `.env`: `INGESTION_INTERVAL=00:15:00`

## Commands (local dotnet run)

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

## Configuration & secrets

Never commit secrets. Connection strings and keys go in **user-secrets** (dev) and environment
variables / Azure App Configuration + Key Vault (deployed). `appsettings.json` holds only non-secret
defaults. All settings are bound via the options pattern with startup validation.

## Data source

- Base URL: `https://api.energidataservice.dk/dataset/{DatasetName}` (public, free, no API key).
- Default dataset: `Gasflow` (daily Danish gas flow), configurable so it can be swapped.
- The API is rate limited and returns HTTP 429 when exceeded; the ingestion client respects the
  retry hint and backs off. We never poll faster than the dataset's update frequency.

## Roadmap

Built milestone by milestone (see [`AGENTS.md`](AGENTS.md) section 16): scaffold -> domain &
persistence -> external client -> ingestion worker -> BFF API -> Blazor dashboard -> observability.

## CI

CircleCI ([`.circleci/config.yml`](.circleci/config.yml)) runs on every push and PR: restore,
`dotnet format --verify-no-changes`, `dotnet build -c Release` (warnings as errors), and
`dotnet test -c Release` with JUnit test reports. Integration tests run on the CircleCI `machine`
executor because Testcontainers needs a local Docker daemon reachable over `localhost`.

## License / attribution

Data provided by Energinet via the Energi Data Service. Source: Energinet
(www.energidataservice.dk).
