# Architecture

> **Status: partially implemented.** This is the target design agreed during the architecture planning phase (see `docs/DECISIONS.md` ADR-002, ADR-003, ADR-004). Phase 2 (Sleeper Experience) — Domain, Persistence, Application, and API — is fully built; see `PROJECT_CONTEXT.md` for exact current state. Everything else described below (Frontend — now its own Phase 3, all of Phase 5 and Phase 6 — renumbered per ADR-011) remains the target, not yet built.

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

- **UserProfile** — domain entity, persistence-ignorant POCO, related 1:1 to the ASP.NET Core Identity user via a foreign key to the Identity user's ID (never by inheritance). Holds LeagueLens-specific data (e.g. linked Sleeper user ID, display preferences); Identity itself is an infrastructure/persistence concern and is never referenced by domain code. See ADR-005.
- **League** — first-class, shared entity keyed by Sleeper league ID. Synced once, not per-user.
- **LeagueMembership** — links `User` ↔ `League` via the user's Sleeper user ID (many-to-many). This is what makes a shared League model work: if 10 LeagueLens users are in the same real Sleeper league, it's synced once and all 10 see it.
- **Player** — global, first-class entity (canonical ID, name, position, team), synced independently of any league. Bootstrapped from Sleeper's player list in Phase 2 (Sleeper Experience) so `Roster` has something to reference; enriched with additional sources starting Phase 5 (Unified Player Profiles). Never duplicated onto league-scoped rows — see ADR-003.
- **Roster / Matchup** — synced from Sleeper per League, current season only for Phase 2. `Roster` references `Player` by FK rather than duplicating player metadata. `Matchup` has no home/away concept — each side is a `MatchupParticipant` row (see ADR-007). Standings, power rankings, and trends computed from this data are Phase 6 (Analytics) scope, not Phase 2 — see ADR-006 and ADR-009. A persisted `StandingSnapshot` is deferred to Phase 6 as well, once true multi-season history makes recomputing from raw matchup history on every read impractical.
- **PlayerSourceRecord** — persisted entity, one row per Player × Source (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros, extensible). Holds both a raw payload (verbatim provider response — source of truth for debugging/reparsing) and a normalized payload (application-facing). Introduced in Phase 5.
- **PlayerProfile** — not persisted. A read-time DTO composed by a dedicated service from a `Player` and its `PlayerSourceRecord`s, exposing single- and batch-composition paths so multi-player reads (rosters, matchups, search, comparison) never trigger N+1 queries. This is the primary read model for player data app-wide. Introduced in Phase 5.

Multi-source player data (trade values, expert rankings) is deferred to Phase 5 (Unified Player Profiles) — Phase 2 only needs `Player` as a lightweight identity for `Roster` to reference. See ADR-003, ADR-004.

### Sync

An externally-triggered worker runs hourly-or-daily, pulls every League with at least one active membership, upserts roster/matchup/standings. No real-time/live-game polling. Sync is organized around providers rather than domain entities — see "Provider Abstraction" below.

### Access model

Most league content is **public and read-only** via shareable links — no login required. Login is only needed to connect your own Sleeper account or personalize the experience.

### API design

REST, stateless. `IConfiguration` for all environment-specific values; no hard-coded cloud SDK calls in controllers/domain logic.

### Testing

Real unit tests on sync/analytics/domain logic, integration tests on the API — not exhaustive coverage chasing. The test project is scaffolded as its own early milestone (Milestone 1.5, right after the Domain layer), not bundled in or deferred indefinitely — see ADR-005.

### Layering conventions

Domain entities are persistence-ignorant POCOs — no EF Core attributes or base classes. Table mapping, keys, relationships, and constraints are configured in the Persistence layer via Fluent API (`IEntityTypeConfiguration<T>`), never via data annotations on domain classes. Structured logging (`ILogger<T>`, structured message templates, not string concatenation) is used from the first milestone, not retrofitted later. `CancellationToken` propagates through async operations where appropriate, per modern ASP.NET Core practice.

### Provider Abstraction

Every external data source (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros, and any future addition) will eventually implement a common provider interface, so a new provider can be added without touching the rest of the application. That interface is *extracted*, not designed upfront: Phase 2 ships Sleeper sync as a concrete, non-abstracted module (a single implementation gives no real evidence of what should be abstracted); the interface is introduced in Phase 5, once a second provider is actually being added. Once it exists, each provider module owns its own authentication (if applicable), parsing, sync scheduling, error handling, and rate limiting independently — sync is organized around providers, not domain entities. Raw provider payloads are preserved alongside normalized representations everywhere sync happens, starting in Phase 2 — this isn't gated on multiple providers existing. See ADR-004.

### Player Profile

The unified player view — compiled data from every source, sectioned by provenance — is the app's flagship feature (see ADR-003). `PlayerProfile` is composed server-side by a dedicated service from `Player` + `PlayerSourceRecord`s; the UI only renders what that service returns, sectioned by source, from a universal entry point (any player reference, on any screen). Identity resolution — matching an incoming source record to the correct `Player` — produces resolved / low-confidence / unresolved states; low-confidence and unresolved matches go to a future admin review interface for manual approve/reject/correct, which is intentionally minimal and functional (a data-quality tool, not a polished screen). Automated matching is a first pass; AI-assisted suggestions are a possible future enhancement; human review is always the authoritative fallback. Each `PlayerSourceRecord` degrades gracefully on provider failure — retaining last-successful data and a staleness indicator rather than going blank, and never affecting other sources in the same composed profile. The future comparison tool (Phase 6) reuses `PlayerProfile` directly. UI pattern (modal vs. side panel) is intentionally undecided pending the Figma design.

## 2. Deployment target: Azure (current, not a hard dependency)

| Component | Azure realization | Why it stays swappable |
|---|---|---|
| SPA | Azure Static Web Apps (free tier) | Same static build output works on Netlify/Vercel/Cloudflare Pages |
| API service | Azure Container Apps, consumption plan (free grant) | Same container image runs on Fly.io/Render/AWS Fargate/etc. |
| Sync worker | Azure Container Apps Jobs, cron-triggered | Same container; only the trigger mechanism changes elsewhere |
| Database | Azure SQL Database (free tier) | EF Core provider swap + connection string, not a rewrite |
| IaC | Bicep | Azure-native; inherently non-portable, but IaC gets rewritten on any cloud change regardless of which tool authored it |

Honest limit of "portable" here: the **code** is portable, the **free-tier economics are not**. Azure SQL's free tier and Container Apps' free grant are Azure-specific pricing structures — moving clouds later means re-evaluating cost, not rewriting the app.

Known operational characteristic: consumption-tier compute (Container Apps) and serverless-tier SQL may cold-start after idle. This is handled at deploy time (e.g. loading states, tier tuning) if it matters in practice — not a driver of the application architecture (see ADR-003).

Deploys are manual for now (no CI/CD yet — deferred, see `docs/ROADMAP.md`).

## 3. Local development (Docker)

| Piece | Approach | Why |
|---|---|---|
| API service | `Dockerfile` (multi-stage .NET build) | Same image later deployed to Container Apps — not duplicated effort |
| Sync worker | `Dockerfile`, same pattern | Same reasoning, once the worker project exists |
| Local database | SQL Server container via `docker-compose.yml` | Reproducible local backend, no dependency on a live Azure SQL connection, zero "works on my machine" drift |
| Angular client | **Native** — `ng serve`, not containerized | Containerizing frontend dev servers is atypical: hot-reload/file-watching is meaningfully slower through Docker's bind-mount layer (especially on Windows), and the SPA has no environment-specific dependencies the way API+DB do |

`docker-compose.yml` at the repo root orchestrates API + local DB (+ worker later); `docker compose up` gets a working backend in one command. Config for local containers lives in compose/env files, not application code.

**Sequencing note:** SQL Server Express (installed directly, not containerized) is used for local development ahead of Phase 4 (Infrastructure — promoted to its own phase per ADR-011), so EF Core migrations can be authored and validated during the Persistence layer without waiting on `docker-compose.yml`. Docker remains the target for reproducible local dev and is added when Phase 4 lands — see ADR-005.

## What's out of scope for the current roadmap

Real-time/live-game sync and billing are rejected/deferred indefinitely (ADR-002) — not scheduled in any phase. CI/CD automation (GitHub Actions) remains deferred; deploys stay manual at least through Phase 4 (Infrastructure), revisited once there's more to protect with automated checks. Trade/roster analysis (multi-source player data) is **not** out of scope — it's Phase 5, Unified Player Profiles (ADR-003, ADR-004). Standings, power rankings, in-season trends, and matchup recaps/highlights are **not** out of scope either — they moved from Phase 2 to Phase 6 (Analytics) per ADR-009; Phase 2 is now scoped to core Sleeper data plumbing only (browsing UI is Phase 3, Frontend, per ADR-011). True multi-season history is also Phase 6. See `docs/ROADMAP.md`.
