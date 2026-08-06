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

**Update (2026-08-04):** Superseded in part by ADR-009 — standings/power-rankings/trends themselves (not just `StandingSnapshot`) moved from Phase 2 to Phase 4, and the code built against this ADR's "computed at query time" design was removed rather than carried forward. The query-time-vs-snapshot question this ADR answered is moot until Phase 4 restarts this work, at which point it should be revisited fresh rather than assumed.

---

## ADR-007: `Matchup` has no Home/Away concept; two-sided results modeled as `MatchupParticipant` rows

**Date:** 2026-08-03
**Status:** Accepted

**Decision:** `Matchup.HomeLeagueMembershipId`/`AwayLeagueMembershipId`/`HomeScore`/`AwayScore` are removed. `Matchup` is now just `{ Id, LeagueId, Week }`; each side's team and score lives in a new `MatchupParticipant` row (`{ Id, MatchupId, LeagueMembershipId, Score }`), two per matchup, with a unique `(MatchupId, LeagueMembershipId)` index. Neither participant row is structurally privileged — there is no first/second, home/away, or any other ordering baked into the schema. `MatchupMapper` and `MatchupSyncer` were updated accordingly; `LeagueIntelService`'s standings/power-ranking/trend calculations were already symmetric across both sides of a matchup and became simpler as a result (no more duplicated home/away accumulation calls). This was a destructive migration (`RemoveHomeAwayFromMatchup`) with no backfill — acceptable since all `Matchup` data is fully reproducible from a Sleeper re-sync, and the project has no production data yet.

**Why:** Sleeper's own API has no home/away concept — `SleeperMatchupDto` is just `{ RosterId, MatchupId, Points }`, with two rows sharing a `matchup_id` and no side label. The original `HomeLeagueMembershipId`/`AwayLeagueMembershipId` split was invented in `MatchupMapper` (`entries[0]` → home, `entries[1]` → away) purely as an artifact of how the mapping code happened to iterate a grouped list — it never represented anything Sleeper reported or anything LeagueLens's business domain cares about. Fantasy football matchups don't have a home field. Carrying that fabricated distinction into the persisted domain model risked it calcifying into "real" meaning as more features were built on top of it (e.g. a recap API that returns `home`/`away`/`tie`), which is exactly what started happening in Milestone 4b before this refactor. Removing it now, while `Matchup` has exactly one consumer layer (`LeagueIntelService`, `WeekRecapService`) and no production data, is far cheaper than removing it later.

**Consequences:** `WeekRecapService`'s response shape (`MatchupRecapEntry` with `HomeTeamName`/`AwayTeamName`/`Winner: home|away|tie`) initially kept the home/away framing, deriving it from an arbitrary API-layer tie-break rather than a domain fact, as a deliberate short-term compromise to land this refactor as one fully-compiling, fully-passing commit rather than a partially-broken one.

**Update (2026-08-03):** the deferred API redesign above is done. `MatchupRecapEntry` is now `{ TeamA: MatchupParticipantResult, TeamB: MatchupParticipantResult, MarginOfVictory }`, where each `MatchupParticipantResult` (`{ LeagueMembershipId, TeamName, Score, Outcome }`) carries its own `MatchupOutcome` (`Win`/`Loss`/`Tie`) — no side is privileged, and the word "home"/"away" no longer appears anywhere in `LeagueLens.Application` or its API surface (verified by grep; only the immutable migration history and an explanatory comment in `MatchupMapper` still mention the removed concept, both expected). `TeamA`/`TeamB` are generic, neutral slot labels, not a real distinction — which participant lands in which slot is decided by the same stable-sort-for-display tie-break used everywhere else in this API (team name, then membership ID), purely so repeated requests serialize identically. An earlier draft of this redesign used a `Participants: MatchupParticipantResult[]` array instead of named slots; changed to `TeamA`/`TeamB` for API ergonomics — named fields are easier for clients to consume than indexing into a fixed-length array.

**Alternatives considered:**
- Renaming the columns to something neutral but still flat (e.g. `TeamOneMembershipId`/`TeamTwoMembershipId`) — rejected: still bakes an arbitrary two-slot ordering into the schema, just relabeled; doesn't actually remove the fabricated distinction, only its name.
- Leaving `Matchup` as-is and only changing the API response shape — rejected: doesn't address the actual concern, which is that the domain/persistence layer itself shouldn't encode a distinction the business domain doesn't have.

---

## ADR-008: On-demand live Sleeper reads are allowed for current-week previews; still no background live-game sync

**Date:** 2026-08-03
**Status:** Accepted

**Decision:** `LeaguesController`'s new `GET weeks/preview` endpoint (Milestone 4d) calls the Sleeper API live, once per request, via `IWeekPreviewService` → `ISleeperApiClient` — the first endpoint in this API that reads anything beyond the local database. This is scoped narrowly: an on-demand read triggered by an individual HTTP request, going through the existing `ISleeperApiClient` abstraction (never raw `HttpClient` calls from the controller or service), with no new background job, scheduled task, or polling loop. Sleeper API failures are caught in the controller and translated to a `502 Bad Gateway` `ProblemDetails` response (`SleeperUnavailable()`, mirroring the existing `LeagueNotFound`/`WeekNotFound` helpers) rather than surfacing a raw exception or generic 500.

**Why this doesn't conflict with ADR-002:** ADR-002 rejected "near-real-time/live-game sync" for v1 — specifically a recurring background worker continuously polling Sleeper to keep persisted data fresh during live games, called out there as "incompatible with the free compute budget." That is a fundamentally different resource-usage pattern than this milestone: a background poller runs continuously regardless of traffic; this endpoint only calls Sleeper when a client actually requests a preview, at most once per request. ADR-002's own v1 feature scope explicitly includes "matchup previews & recaps" — this milestone is that scope item, not a reversal of the earlier rejection. The distinction that matters is *continuous background polling* (rejected) vs. *on-demand request-triggered reads* (allowed) — not "live data" vs. "not live data" in general.

**Why live reads are needed at all:** `SleeperSyncService` deliberately never persists the current (in-progress/upcoming) week — `throughWeek = state.Week - 1` — so partial/live scores never distort standings (see the sync service's own comment). Roster ownership (`roster_id` → Sleeper user ID) also isn't persisted anywhere in the domain model — only roster *composition* is (`Roster` entity). A current-week preview therefore has no persisted source to read from at all; a live call is the only option, not a shortcut around the existing sync pipeline.

**Scope for this milestone specifically:** bare team pairing only — no scores (the games haven't happened), no standings/power-ranking context, no projections, no AI-generated text. The goal is establishing that the live-read pattern works reliably end-to-end (fetch, map, error-handle) before building anything richer on top of it in a later milestone.

**Alternatives considered:**
- Extending `SleeperSyncService` to also persist the current week's pairing — rejected for this milestone: reopens the exact partial/live-score risk the sync layer was deliberately built to avoid, and turns a read-only API feature into a persistence/migration change for no immediate benefit.
- Enriching the preview with standings/power-ranking context in the same milestone — rejected for now: conflates "does the live-read pattern work" with "what should a rich preview contain," better validated separately. Revisit once this pattern is proven.

**Update (2026-08-04):** Per ADR-009, `IWeekPreviewService`/`WeekPreviewService` and its DTOs were relocated from `LeagueLens.Application/LeagueIntel/` to `LeagueLens.Application/Sleeper/Preview/`, and registered via `AddSleeperSync()` instead of the now-removed `AddLeagueIntel()`. This is a namespace/registration move only — the live-read pattern and rationale this ADR documents are unchanged. The relocation reflects that this service performs no analytics (no scores, no derived stats, no predictions), so it belongs with core Sleeper plumbing rather than with the analytics code that moved to Phase 4.

---

## ADR-009: Roadmap restructured around Sleeper Platform before Analytics; League Intel analytics code removed

**Date:** 2026-08-04
**Status:** Accepted

**Decision:**
- Phase 2 is retitled from "League Intel" to "Sleeper Platform." Its scope changes from computing analytics (standings, power rankings, trends, matchup recaps/highlights) over Sleeper data to shipping a polished, usable application over Sleeper data: importing/persisting leagues, rosters, players, and matchups; exposing clean REST read endpoints for them; and building the corresponding frontend (league/roster/player/team/matchup pages, player search, draft/transactions if Sleeper supports them).
- Everything analytics-flavored — standings, power rankings, in-season trends, matchup recaps, week highlights — moves from Phase 2 to the new Phase 4 (Analytics). Phase 3 (Unified Player Profiles) is unchanged and remains the flagship feature, unchanged in position or substance.
- The already-built analytics code for this (Milestones 3b, 4a–4c: `LeagueIntelService`, `IPowerRankingCalculator`/`BlendedPowerRankingCalculator`, `LeagueIntelOptions`, `WeekRecapService`, `WeekHighlightsService`, their DTOs, and the corresponding `LeaguesController` endpoints/tests) is **removed outright**, not relocated-in-place or feature-flagged off. It will be rebuilt from scratch when Phase 4 starts.
- `WeekPreviewService` (Milestone 4d, the `GET /preview` endpoint) is the one exception: it's kept, relocated from `LeagueLens.Application/LeagueIntel/` into `LeagueLens.Application/Sleeper/Preview/`, and registered via `AddSleeperSync()` instead of the deleted `AddLeagueIntel()` (see ADR-008 update). It returns only concrete Sleeper data (live current-week team pairings, no scores or derived stats), so it fits Phase 2's "matchup pages" scope rather than analytics.

**Why:** The product's actual differentiator was always the unified, multi-source Player Profile (ADR-003), not league analytics — but the original roadmap sequenced a full analytics engine on Sleeper-only data as Phase 2, ahead of any player browsing, search, or profile UI. That ordering reflected what was easiest to build first from Sleeper's API, not the product's stated value proposition. Removing the already-built analytics code, rather than preserving it in a "deferred" state, was deliberate: the eventual analytics approach (which stats, what formulas, what the frontend needs) may look different by the time Phase 4 actually starts, once real Player Profile and Sleeper Platform UI exist to inform it — so there's little value preserving code shaped by requirements that may no longer hold, and rebuilding fresh against then-current requirements is cheaper than maintaining or migrating code that isn't being exercised in the meantime.

**Alternatives considered:**
- Leaving the analytics code in place and just redocumenting it as "Phase 4 work, built ahead of schedule" — rejected: preserving now-possibly-wrong-shaped code has no clear payoff if the analytics approach changes before Phase 4 starts, and a clean Phase 2 codebase is worth more than salvaging tested-but-premature code.
- Physically relocating the analytics code into a clearly-labeled "deferred" folder instead of deleting it — rejected for the same reason; still carries code that may not match Phase 4's eventual shape.
- Also removing `WeekPreviewService`/`/preview` along with the rest of `LeagueIntel/` — rejected: it performs no analytics (no scores, no derived stats, no predictions), just pairs teams from live Sleeper data, so it fits "matchup pages" as scoped for Phase 2's Sleeper Platform.

**Update (2026-08-06):** Per ADR-010, Phase 2 is retitled again, from "Sleeper Platform" to "Sleeper Experience" — the underlying scope this ADR defines (a polished, read-only application over Sleeper data, no analytics) is unchanged; only the name changes, to avoid "Platform" implying LeagueLens is building a competing base for league data rather than a companion view of it.

---

## ADR-010: LeagueLens is a companion to Sleeper, not a replacement for it

**Date:** 2026-08-06
**Status:** Accepted

**Decision:** LeagueLens is explicitly scoped as a companion to Sleeper — a permanent product boundary, not a temporary limitation. Sleeper is the system of record: league metadata, teams, rosters, matchups, and transactions all originate from and are managed in Sleeper. LeagueLens enriches that data; it does not own, manage, or replicate Sleeper's league-management functionality. This holds even if Sleeper's API were to gain write access in the future — the boundary is intentional, not a byproduct of what the API currently allows.

LeagueLens's job is (a) a read-only view of a user's Sleeper team/league and (b) the compiled, multi-source Player Profile (ADR-003) reachable by clicking any player anywhere in the app — bringing together rankings, trade values, news, and analysis that today are scattered across multiple separate sites into one place.

**Guiding product vision:** Success is measured by how quickly a user can go from seeing a player in their Sleeper league to understanding that player's value through aggregated information from multiple trusted sources. Future feature decisions should be weighed against this — does it shorten that path, or does it drift LeagueLens toward reimplementing something Sleeper already does?

**Why:**
1. **Product focus (primary reason):** ADR-009 already established the Player Profile, not league mechanics, as the product's actual differentiator. Sleeper already handles league management; LeagueLens's value is aggregating the player research that's normally spread across multiple separate services into one unified view. Building league-management features would dilute that focus rather than add to it.
2. **Sleeper as system of record:** Treating Sleeper as the sole authoritative source for league/roster/transaction data — rather than something LeagueLens duplicates or takes over — keeps the data model honest: LeagueLens syncs and enriches, it never forks a second, divergent copy of league state.
3. **Supporting context, not the rationale:** Sleeper's public API (`docs.sleeper.com`) is currently read-only, which reinforces this direction today, but that's incidental — the boundary is a deliberate product choice that would hold even if Sleeper exposed write endpoints tomorrow.

This positioning had never been stated anywhere in the docs before this ADR — an audit of `README.md`, `PROJECT_CONTEXT.md`, and `docs/ROADMAP.md` found no contradiction, just silence, plus a couple of ambiguous phrases (e.g. ROADMAP's former "a polished, usable fantasy football application") that could be misread as LeagueLens replacing Sleeper's own app rather than complementing it.

**Alternatives considered:**
- Building lineup-setting/waiver-processing UI in LeagueLens — rejected: off-product-focus, and would mean LeagueLens managing data it doesn't own.
- Framing this as an API limitation rather than a product decision — rejected: that would leave the boundary implicitly reversible if Sleeper ever added write access, when the intent is for LeagueLens to stay a companion regardless.
- Leaving the positioning unstated (status quo) — rejected: the ambiguity in existing docs was actively capable of misleading a reader about what LeagueLens does.

**Related rename:** Phase 2 is retitled from "Sleeper Platform" (per ADR-009) to **"Sleeper Experience"** — "Platform" reads as LeagueLens building its own competing base for league data, which contradicts the companion positioning this ADR establishes; "Experience" better describes a polished, read-only view layered on Sleeper's own data. See `docs/ROADMAP.md`.

---

## ADR-011: Frontend and Infrastructure promoted to standalone phases (3 and 4), ahead of Unified Player Profiles

**Date:** 2026-08-06
**Status:** Accepted

**Decision:**
- Phase 2 ("Sleeper Experience") is rescoped down to just its Domain → API layers (Milestones 1–4) — all of which are done — and is now marked ✅ Done in full.
- Its two remaining sub-items are promoted out of Phase 2's internal layer numbering into their own top-level phases, in this order: **Phase 3 — Frontend**, **Phase 4 — Infrastructure**.
- Every later phase renumbers up by two: former Phase 3 (Unified Player Profiles) → **Phase 5**, former Phase 4 (Analytics) → **Phase 6**, former Phase 5 (AI Assistant) → **Phase 7**.

**Why:** No technical dependency requires Infrastructure before Frontend — local development already runs against SQL Server Express directly rather than the still-unbuilt Docker setup (ADR-005), so nothing about deployment tooling blocks building and testing the Angular UI locally against the now-complete API. Sequencing Frontend first means real UI work can start the moment the Figma design lands, without waiting on unrelated deployment plumbing. Frontend followed immediately by Infrastructure also produces a genuinely useful checkpoint — a deployed, usable companion app — before committing to Phase 5's larger scope (multi-provider scraping, identity resolution, admin review tooling), which is a better portfolio milestone to reach than "API only, no UI, not deployed."

This is a scope redefinition, not evidence of new completed work: Phase 2 isn't more done than it was yesterday — it's just no longer defined to include Frontend/Infrastructure as sub-items, so its actual (unchanged) completion state can be marked accurately.

**Consequences:**
- Phase 5 (Unified Player Profiles) keeps its own six-layer breakdown, including its own "Infrastructure" (scraper-specific deployment needs) and "Frontend" (click-to-detail UI) sub-items — those describe that phase's own feature-specific work and are distinct from the foundational deployment infra now being built in the standalone Phase 4.
- Phase 5 now starts once Phase 4 ships (i.e., after both Frontend and Infrastructure), not once Phase 2 ships — Phase 2 alone is no longer a meaningful gate now that it's already done.

**Alternatives considered:**
- Leaving Frontend/Infrastructure as Phase 2's layers 5/6 and just marking Phase 2 "mostly done" — rejected: doesn't reflect that these two are now being pursued as full-scope, prioritized work in their own right, not afterthought cleanup items on an already-shipped phase.
- Infrastructure before Frontend (the original ordering) — rejected: no technical dependency forces this order, and Frontend first gets a testable UI sooner, with Infrastructure/deployment following once there's something real to deploy.
