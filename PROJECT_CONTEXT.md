# Project Context

*Current state only. For durable guidance see `CLAUDE.md`; for decision history see `docs/DECISIONS.md`.*

**What it is:** LeagueLens — a companion app for Sleeper dynasty fantasy football leagues, and a portfolio piece for demonstrating full-stack engineering skill. Started as a side project to explore fantasy football data while leveling up cloud/full-stack skills. Sleeper remains the system of record for leagues, rosters, matchups, and transactions (ADR-010) — LeagueLens's core value proposition is the **unified Player Profile** — one canonical view per player, compiling data pooled from every source (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros, and more over time) into clearly sectioned, provenance-labeled data, reachable from any player reference anywhere in the app.

**Stage:** Foundation and architecture planning are complete (see `docs/ARCHITECTURE.md`, `docs/DECISIONS.md`). Phase 2 (Sleeper Experience — retitled and rescoped from "League Intel" per ADR-009, renamed from "Sleeper Platform" per ADR-010) is underway:

- **Milestone 1 (Domain — `LeagueLens.Domain`)** — done. `UserProfile`, `League`, `LeagueMembership`, `Roster`, `Matchup`, `MatchupParticipant`, `Player` as persistence-ignorant POCOs.
- **Milestone 1.5 (test project — `LeagueLens.Domain.Tests`)** — done.
- **Milestone 2 (Persistence — `LeagueLens.Persistence`)** — done. EF Core `DbContext`, Identity integration, Fluent API configs, migrations validated against local SQL Server Express per ADR-005.
- **Milestone 3 (Application — `LeagueLens.Application/Sleeper`)** — done. Sleeper API client, DTOs, pure mappers, delete-and-replace/upsert syncers orchestrated by `SleeperSyncService`, wired into `LeagueLens.Server` via `AddSleeperSync()`. Also includes `WeekPreviewService` (current week's live matchup pairings, no scores — see ADR-008) and `SyncLeagueWithCatalogRefreshAsync` — the single orchestrated sync action (player catalog + league, each independently cooldown-gated via a new `SyncStatus` table; cooldowns configurable via the `SleeperSync` section of `appsettings.json`, defaulting to 60/10 minutes).
- **Milestone 4 (API — `LeaguesController`)** — partially done. Ships `GET /api/leagues/{sleeperLeagueId}/preview` and `POST /api/leagues/{sleeperLeagueId}/sync`. No raw read endpoints exist yet for leagues/rosters/players/matchups.

**Removed (2026-08-04, per ADR-009):** Milestones formerly numbered 3b and 4a–4c under the old "League Intel" framing — `LeagueIntelService` (standings/power-rankings/trends), `WeekRecapService`, `WeekHighlightsService`, their DTOs, and the corresponding `LeaguesController` endpoints/tests — were built, then removed outright rather than kept in place, since the product's roadmap was restructured to put a usable Sleeper-only application (Phase 2) and the unified Player Profile (Phase 3) ahead of analytics (now Phase 4), and the eventual analytics approach may look different by the time Phase 4 actually starts. `WeekPreviewService`/`GET /preview` was the one piece kept, relocated from `LeagueLens.Application/LeagueIntel/` to `LeagueLens.Application/Sleeper/Preview/` since it performs no analytics (just live team pairings from Sleeper).

**Also done (outside the numbered milestone list):** a domain-model refactor removing Home/Away framing from `Matchup` entirely (ADR-007) — `Matchup` is now `{Id, LeagueId, Week}` with `MatchupParticipant` rows; applied via destructive migration with no backfill.

**Caveats/notes flagged in the doc itself:**
- The renamed scaffold's client/server wiring (SPA proxy, single-deployable static file serving) is being replaced per the planned architecture — API and SPA will deploy separately. "Don't treat the current `Program.cs`/`proxy.conf.js` setup as the target design."
- **Frontend:** a Figma design exists but hasn't been shared into the repo yet. No frontend implementation work starts until it is.

**Next:** the remaining Phase 2 (Sleeper Experience) gap is raw read endpoints for leagues/rosters/players/matchups — see `docs/ROADMAP.md` for full phase detail.
