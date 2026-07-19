# Architecture

> **Status: planned, not yet implemented.** This is the target design agreed during the architecture planning phase (see `docs/DECISIONS.md` ADR-002). The repo currently only contains the renamed foundation scaffold — no application code exists yet.

Three separate concerns, kept deliberately distinct: what the app *is* (portable), how it's *deployed today* (Azure, swappable), and how it's *run locally* (Docker, for reproducibility).

## 1. Application architecture (cloud-agnostic)

| Component | What it is | Portability |
|---|---|---|
| SPA | Static built Angular assets | Deployable to any static host/CDN |
| API service | Stateless ASP.NET Core Web API, containerized | Standard Docker image, runs on any container host |
| Sync worker | Separate deployable unit; does one sync pass and exits, triggered externally on a schedule | Only needs *something* to invoke it on a cron |
| Database | Relational, accessed only via EF Core | Provider-swappable (SQL Server ↔ Postgres) as long as no vendor-specific SQL leaks into app code |
| Auth | ASP.NET Core Identity + JWT | Framework-level, self-hosted, no cloud dependency |

**Governing principle:** vendor-specific code (cloud SDKs, provider config) stays confined to the outermost infrastructure/configuration layer. Domain and business logic depend only on `IConfiguration`, EF Core, and plain `HttpClient` — never a cloud SDK directly.

### Data model (conceptual)

- **User** — LeagueLens account (ASP.NET Core Identity)
- **League** — first-class, shared entity keyed by Sleeper league ID. Synced once, not per-user.
- **LeagueMembership** — links `User` ↔ `League` via the user's Sleeper user ID (many-to-many). This is what makes a shared League model work: if 10 LeagueLens users are in the same real Sleeper league, it's synced once and all 10 see it.
- **Roster / Matchup / StandingSnapshot** — synced from Sleeper per League, current season only for v1.

No player-level stat granularity in v1 — that belongs to the deferred trade-analysis feature.

### Sync

An externally-triggered worker runs hourly-or-daily, pulls every League with at least one active membership, upserts roster/matchup/standings. No real-time/live-game polling in v1.

### Access model

Most league content is **public and read-only** via shareable links — no login required. Login is only needed to connect your own Sleeper account or personalize the experience.

### API design

REST, stateless. `IConfiguration` for all environment-specific values; no hard-coded cloud SDK calls in controllers/domain logic.

### Testing

Real unit tests on sync/analytics/domain logic, integration tests on the API — not exhaustive coverage chasing.

## 2. Deployment target: Azure (current, not a hard dependency)

| Component | Azure realization | Why it stays swappable |
|---|---|---|
| SPA | Azure Static Web Apps (free tier) | Same static build output works on Netlify/Vercel/Cloudflare Pages |
| API service | Azure Container Apps, consumption plan (free grant) | Same container image runs on Fly.io/Render/AWS Fargate/etc. |
| Sync worker | Azure Container Apps Jobs, cron-triggered | Same container; only the trigger mechanism changes elsewhere |
| Database | Azure SQL Database (free tier) | EF Core provider swap + connection string, not a rewrite |
| IaC | Bicep | Azure-native; inherently non-portable, but IaC gets rewritten on any cloud change regardless of which tool authored it |

Honest limit of "portable" here: the **code** is portable, the **free-tier economics are not**. Azure SQL's free tier and Container Apps' free grant are Azure-specific pricing structures — moving clouds later means re-evaluating cost, not rewriting the app.

Deploys are manual for now (no CI/CD yet — deferred, see `docs/ROADMAP.md`).

## 3. Local development (Docker)

| Piece | Approach | Why |
|---|---|---|
| API service | `Dockerfile` (multi-stage .NET build) | Same image later deployed to Container Apps — not duplicated effort |
| Sync worker | `Dockerfile`, same pattern | Same reasoning, once the worker project exists |
| Local database | SQL Server container via `docker-compose.yml` | Reproducible local backend, no dependency on a live Azure SQL connection, zero "works on my machine" drift |
| Angular client | **Native** — `ng serve`, not containerized | Containerizing frontend dev servers is atypical: hot-reload/file-watching is meaningfully slower through Docker's bind-mount layer (especially on Windows), and the SPA has no environment-specific dependencies the way API+DB do |

`docker-compose.yml` at the repo root orchestrates API + local DB (+ worker later); `docker compose up` gets a working backend in one command. Config for local containers lives in compose/env files, not application code.

## What's explicitly out of scope for v1

Trade/roster analysis (needs KeepTradeCut/FantasyCalc/FantasyPros scraping — a separate, decoupled scraper module with cache-on-failure behavior), true multi-season history, real-time/live scoring, billing, CI/CD automation. See `docs/ROADMAP.md`.
