# Decisions

Lightweight architecture decision log. One entry per significant decision: what was decided, why, and what else was considered.

---

## ADR-001: Establish LeagueLens as a renamed, cleaned foundation

**Date:** 2026-07-18
**Status:** Accepted

**Decision:** Renamed the repo/solution from the Visual Studio scaffold name `SleeperApp` to `LeagueLens` (projects, namespaces, GitHub repo, docs), removed the WeatherForecast demo code, and added a minimal docs structure (`README.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/DECISIONS.md`, `PROJECT_CONTEXT.md`).

**Why:** LeagueLens is intended as a portfolio flagship project and needed a clean, professionally-named foundation before any real feature work begins, rather than building on top of scaffold-generated names and demo code.

**Alternatives considered:** Keeping the `SleeperApp` name and renaming later — rejected, since every additional commit under the old name is more to rewrite later for no benefit.

---

## ADR-002: v1 application architecture and scope

**Date:** 2026-07-18
**Status:** Accepted

**Decision:** Full architecture defined in `docs/ARCHITECTURE.md`. Summary of the significant calls:

- **Data source:** Sleeper's public API (no auth required). Scraping KeepTradeCut/FantasyCalc/FantasyPros for trade values and expert rankings is planned but deferred to v2.
- **Tenancy model:** `League` is a first-class, shared entity — synced once per real Sleeper league and viewable by every LeagueLens user who's a member, rather than each user getting a private copy of the same data.
- **Access:** Public, read-only league pages via shareable links; login only required to connect an account or personalize.
- **Auth:** ASP.NET Core Identity + JWT, self-hosted.
- **v1 feature scope:** standings & power rankings, in-season trends (weekly points/power-ranking movement, in-season head-to-head — not multi-year), matchup previews & recaps. Trade/roster analysis, true multi-season history, real-time sync, and billing are explicitly deferred to v2.
- **Architecture is cloud-agnostic; Azure is the initial deployment target, not a hard dependency.** Vendor-specific code stays confined to the infra/config layer. See `docs/ARCHITECTURE.md` for the full portability breakdown.
- **Database:** relational via EF Core; Azure SQL Database (free tier) as the initial host.
- **Compute:** containerized stateless API + externally-scheduled sync worker; Azure Container Apps (consumption, free grant) + Container Apps Jobs as the initial host. SPA on Azure Static Web Apps (free tier).
- **IaC:** Bicep.
- **Local development:** Docker for the API, sync worker, and local SQL Server, via `docker-compose.yml` — reproducible local backend, and the same container images later used for Azure deployment. The Angular client stays native (`ng serve`), not containerized, for hot-reload performance.
- **CI/CD:** manual deploys for now, automation deferred.
- **Testing bar:** real unit tests on sync/analytics/domain logic and API integration tests — not exhaustive coverage chasing.
- **Monetization:** free product, no billing.

**Why:** This is a solo-developed portfolio flagship project built entirely on free-tier infrastructure. Every decision above was chosen to balance three goals: keep v1 shippable and scoped, demonstrate real engineering judgment (data modeling, cloud-agnostic design, containerization, testing discipline) rather than just feature output, and stay within genuinely-free hosting (not trial credits) so the deployed app remains available indefinitely for portfolio review.

**Alternatives considered:**
- Per-user private league data instead of a shared `League` entity — rejected as wasteful (the same real league's data would be synced and stored redundantly for every member who signs up) and a poor fit for the domain (members of the same league should see identical numbers).
- Fully gated access (login required for everything) — rejected in favor of public read-only pages, since a reviewer being able to see real output without signing up matters for a portfolio piece.
- Near-real-time/live-game sync — rejected for v1 as incompatible with the free compute budget and unnecessary for the standings/trends/recap feature set chosen.
- Terraform over Bicep — rejected since the project is Azure-only today and Bicep has less boilerplate; IaC is deployment-target-specific by nature regardless of tool, so this doesn't compromise the cloud-agnostic application architecture.
- Trade/roster analysis in v1 — rejected despite being the reason KeepTradeCut/FantasyCalc scraping is planned, because it depends on a separate, fragile scraping pipeline that shouldn't block shipping the core sync/analytics pipeline first.

---

## ADR-003: Player as a global domain entity; PlayerProfile as a service-composed DTO

**Date:** 2026-07-19
**Status:** Accepted

**Decision:**
- `Player` is a global, first-class domain entity — synced/resolved independently of any league. It is persisted, and league-scoped entities (e.g. `Roster`) reference it by FK rather than duplicating player metadata (name, position, team).
- `PlayerSourceRecord` is a persisted entity, one row per Player × Source (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros, and any future source), holding both a raw payload (the verbatim provider response — kept as the source of truth for debugging and future parser improvements) and a normalized payload (structured, application-facing).
- `PlayerProfile` is a read-time DTO, not a persisted entity. A dedicated composition service assembles it from a `Player` and its associated `PlayerSourceRecord`s. This service exposes both single-player and batch (multi-player) composition paths — both querying in bulk, not one row at a time — so any feature needing multiple players (rosters, matchups, search, comparison) avoids N+1 query patterns. This composition service is the sole path by which any part of the app reads player data; it's the primary read model app-wide, not scoped to one screen. Isolating composition behind this service is also what makes later transparent caching (starting with ASP.NET Core's built-in `IMemoryCache`) possible without changing controllers or the frontend.
- Identity resolution — matching an incoming per-source record to the correct canonical `Player` — produces one of three states: resolved, low-confidence, or unresolved. Low-confidence and unresolved records surface in a future admin review interface for manual approve/reject/correct. That interface is intentionally minimal and functional — a data-quality tool, not a polished end-user experience. Automated matching is a first pass; AI-assisted match suggestions are a possible future enhancement; human review remains the authoritative fallback in all cases.
- Provider failures degrade gracefully and in isolation: each `PlayerSourceRecord` retains its last-successful data and a staleness indicator even when the most recent fetch attempt fails, and one source's failure never breaks the rest of a composed `PlayerProfile`.
- Cold-start latency from free-tier hosting (Container Apps consumption plan, Azure SQL serverless) is treated as a deployment/ops concern, addressed at deploy time (e.g. loading states, tier tuning) — not a driver of this or any other application-architecture decision.

**Why:** The unified player view — compiled data from every source, sectioned by provenance — is the app's actual differentiator, not a minor feature. Modeling it as entity + DTO composed in a service layer (rather than ad hoc UI-side aggregation) means every future player-data feature gets correct, batch-safe, cache-ready reads for free instead of re-deriving aggregation logic per screen. Designing the entity/DTO split, batch composition, resolution states, and graceful degradation in from the start is inexpensive now and expensive to retrofit once multiple features depend on player data.

**Alternatives considered:**
- Client-side aggregation (UI calls multiple source-specific endpoints and merges results itself) — rejected: pushes identity-resolution logic to the client, duplicates it per screen, and can't be reused cleanly by the future comparison tool.
- `PlayerProfile` as its own persisted/cached table — rejected for now as premature; revisit only if read performance demands it once real usage exists.
- Duplicating player metadata onto `Roster` rows for query convenience — rejected: violates the global-entity principle and creates drift between a league's copy and the canonical record, mirroring why `League` itself is shared rather than duplicated per user (ADR-002).
- Silently guessing on ambiguous cross-source matches instead of surfacing low-confidence/unresolved states — rejected: for a feature whose entire value is trustworthy compiled data, a wrong auto-match is worse than missing data.

---

## ADR-004: Provider abstraction is YAGNI-gated; providers own their own sync; raw and normalized data are both preserved

**Date:** 2026-07-19
**Status:** Accepted

**Decision:**
- Every external data source (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros, and any future addition) will eventually implement a common provider interface, so new providers can be added without touching the rest of the application.
- That interface is *extracted*, not designed upfront: Phase 2 (League Intel) ships Sleeper sync as a concrete, non-abstracted module, since a single implementation gives no real evidence of what should be abstracted. The interface is introduced in Phase 3 (Unified Player Profiles), once KeepTradeCut/FantasyCalc/FantasyPros are actually being added alongside Sleeper and there are ≥2 real implementations to generalize from.
- Synchronization is organized around providers, not domain entities: each provider module owns its own authentication (if applicable), parsing, sync scheduling, error handling, and rate limiting independently, rather than a single entity-centric sync job special-casing every source's quirks in shared code.
- Raw provider payloads are preserved alongside normalized representations everywhere sync happens, starting from Phase 2 — this is not gated on multiple providers existing. The raw payload is the source of truth for debugging and future parser improvements; the normalized representation is what the rest of the application reads.

**Why:** New providers should be addable without touching unrelated code (open/closed principle), but a provider interface designed from a single implementation (Sleeper alone) risks being a poor fit once real differences across providers (auth models, rate limits, failure modes, data shapes) show up — better to let it emerge from actual evidence. Provider-centric sync organization matches how sources actually differ from each other far better than entity-centric sync would. Preserving raw payloads means a parser bug or an upstream schema change can be fixed retroactively against real historical data instead of requiring a full resync.

**Alternatives considered:**
- Designing the common provider interface upfront, before any second provider exists — rejected per YAGNI; an interface extracted from ≥2 real implementations fits actual provider differences better than one speculatively generalized from a single case.
- Entity-centric sync (a single "sync players" job pulling from all sources at once) — rejected: forces every provider's auth/rate-limit/error quirks into shared code instead of isolating them.
- Normalizing on ingest without keeping the raw payload — rejected: a parser bug or upstream change would then be unrecoverable without a full resync.

---

## ADR-005: Identity kept out of the domain layer; test scaffolding sequenced as its own milestone; SQL Server Express ahead of Docker for local dev

**Date:** 2026-07-28
**Status:** Accepted

**Decision:**
- Domain entities never inherit from or otherwise directly reference ASP.NET Core Identity types (`IdentityUser`, `IdentityRole`, etc.). Identity is treated as an infrastructure/persistence concern only, configured in the Persistence/Infrastructure layer.
- A domain `UserProfile` entity is introduced as a persistence-ignorant POCO, related 1:1 to the Identity user via a foreign key to the Identity user's ID — never by inheritance. `UserProfile` holds LeagueLens-specific data (e.g. linked Sleeper user ID, display preferences); the Identity user record exclusively owns credentials/auth.
- Test project scaffolding is its own milestone — **Milestone 1.5** — run immediately after Milestone 1 (Domain layer) and before Persistence, rather than bundled into Domain or deferred indefinitely. Milestone 1 itself ships domain entities only, no test project.
- For local development, SQL Server Express is used directly (not containerized) to validate EF Core migrations during the Persistence milestone. Docker-based local SQL Server (per ADR-002/`docs/ARCHITECTURE.md`) remains the target for the Infrastructure layer milestone later in Phase 2 — it isn't dropped, just no longer a prerequisite for validating migrations.

**Why:**
- `IdentityUser` inheritance would leak a persistence/framework-coupled type into the domain layer, breaking the persistence-ignorant POCO convention (`docs/ARCHITECTURE.md`, "Layering conventions") applied to every other entity. Composing via FK keeps domain logic testable without spinning up ASP.NET Core Identity infrastructure.
- Splitting test scaffolding into its own milestone keeps Milestone 1 tightly scoped to domain modeling while still standing up testing infrastructure deliberately soon after, rather than bundling it in or deferring it indefinitely to "whenever there's time."
- Docker's value is reproducibility and deploy-image parity, not migration correctness; SQL Server Express lets Persistence-layer work start immediately without depending on Infrastructure-layer tooling that's sequenced later for good reason.

**Alternatives considered:**
- `UserProfile` inheriting from `IdentityUser<Guid>` — rejected: couples the domain layer to an ASP.NET Core Identity/EF type.
- Bundling the test project into Milestone 1 — rejected for now in favor of a dedicated, smaller milestone (1.5) directly after it.
- Waiting for `docker-compose` before validating any EF Core migration — rejected: blocks Persistence-layer work on Infrastructure-layer work with no compensating benefit during solo local development.

---

## ADR-006: Standings computed at query time in Phase 2; `StandingSnapshot` deferred to Phase 4

**Date:** 2026-07-28
**Status:** Accepted

**Decision:** `StandingSnapshot` is removed from the Phase 2 (League Intel) domain layer. Phase 2's standings, power rankings, and in-season trends (ADR-002 v1 feature scope) are computed at query time in the Application layer from persisted `Matchup` (and `Roster`) data — current-season scores are already synced weekly, so no additional persisted entity is needed to derive them. A persisted `StandingSnapshot` (or equivalent) entity is deferred to Phase 4 (Analytics), where it earns its keep once true multi-season history is in scope and recomputing standings from every historical matchup on every read becomes wasteful.

**Why:** Phase 2 only needs current-season standings/trends, which are a straightforward aggregation over already-persisted `Matchup` rows — persisting a redundant snapshot table adds write-path complexity (keeping it in sync with `Matchup`) without a corresponding read benefit yet. Deferring it to Phase 4 keeps Phase 2's domain layer smaller and avoids designing a snapshot schema before multi-season requirements (retention, historical comparison) are concrete.

**Alternatives considered:**
- Keeping `StandingSnapshot` in Phase 2 as originally scoped — rejected: no current read pattern needs a persisted snapshot when the source data (`Matchup`) is already there and current-season volume is small.
- Removing standings/power-rankings from Phase 2 scope entirely — rejected: not requested; ADR-002's v1 feature scope is unchanged, only how it's computed shifts.
