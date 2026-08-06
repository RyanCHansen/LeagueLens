# Roadmap

- **Phase 1 — Foundation** ✅ Done. Repo renamed to LeagueLens, demo scaffold removed, baseline docs in place, architecture planning complete (see `docs/ARCHITECTURE.md` and `docs/DECISIONS.md`, ADR-001 through ADR-004).

- **Phase 2 — Sleeper Experience** ⏳ In progress. A polished, read-only view of your Sleeper leagues, rosters, and matchups — no lineup-setting or waiver management (Sleeper stays the system of record for that, per ADR-010), no analytics yet (see Phase 4; retitled and rescoped from "League Intel" per ADR-009, renamed from "Sleeper Platform" per ADR-010). Built as a complete vertical slice, layer by layer:
  1. **Domain** ✅ Done. `UserProfile`, `League`, `LeagueMembership`, `Roster`, `Matchup`, `MatchupParticipant`, and `Player` (lightweight identity, bootstrapped from Sleeper's player list so `Roster` has something to reference) as persistence-ignorant POCOs. `UserProfile` relates 1:1 to the ASP.NET Core Identity user by FK, never by inheritance (ADR-005). `Matchup` carries no home/away concept — each side is a `MatchupParticipant` row (ADR-007).
  1.5. **Testing infrastructure** ✅ Done. `LeagueLens.Domain.Tests` scaffolded with initial tests against the Milestone 1 domain entities, sequenced right after Domain and before Persistence (ADR-005).
  2. **Persistence** ✅ Done. EF Core `DbContext` + Fluent API configurations + migrations for the above, including Identity's own tables (configured as an infrastructure concern, not domain). Validated locally against SQL Server Express directly (not containerized yet — ADR-005).
  3. **Application** ⏳ Partially done. Concrete Sleeper sync module shipped: API client, DTOs, pure mappers, delete-and-replace/upsert syncers orchestrated by `SleeperSyncService`, registered via `AddSleeperSync()` (no provider interface yet — single provider, nothing to abstract). Also shipped: `WeekPreviewService` (current week's live matchup pairings, no scores — see ADR-008, relocated here from the removed League Intel work per ADR-009). Not yet done: a way to trigger a sync over HTTP.
  4. **API** ⏳ Partially done. Shipped: `GET /api/leagues/{sleeperLeagueId}/preview`. Not yet done: a sync-trigger endpoint, and raw read endpoints for leagues/rosters/players/matchups.
  5. **Infrastructure** — not started. `docker-compose.yml` (API + local SQL Server), `scripts/` folder (setup, DB reset, dev data seeding, local startup), Azure deployment via Bicep (Static Web Apps + Container Apps + Container Apps Jobs + Azure SQL), manual deploy.
  6. **Frontend** — not started. Angular UI, following a Figma design (not yet shared) once available; no frontend work starts before that. Scope once started: league pages, roster pages, player pages, player search, team pages, matchup pages, and draft/transaction pages if Sleeper's API supports them well enough.

  **Note:** Milestones 3b and 4a–4c under the old "League Intel" framing (standings/power-ranking/trend calculations and their API endpoints) were built, then removed on 2026-08-04 per ADR-009 — the analytics approach may look different by the time Phase 4 restarts this work, so the code wasn't preserved in place. See git history before that date if ever needed for reference.

- **Phase 3 — Unified Player Profiles**. The flagship feature: a canonical, compiled view of every player, pooled from every source and sectioned by provenance. Starts once Phase 2 ships; repeats the same six-layer sequence:
  1. **Domain** — `PlayerSourceRecord` (raw + normalized payload) as a persistence-ignorant POCO; common provider interface **extracted here**, once KeepTradeCut/FantasyCalc/FantasyPros join Sleeper as real second/third/fourth implementations (see ADR-004).
  2. **Persistence** — EF Core entities/migrations for `PlayerSourceRecord` via Fluent API.
  3. **Application** — `PlayerProfileService` (single + batch composition, cache-ready), per-provider sync modules (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros — each own auth/parsing/error-handling/rate-limiting behind the provider interface), identity resolution (resolved/low-confidence/unresolved states), a minimal/functional admin review interface for manual approve/reject/correct.
  4. **API** — `PlayerProfile` endpoints (single + batch).
  5. **Infrastructure** — scraper-specific deployment needs as they arise.
  6. **Frontend** — universal click-to-detail UI, sectioned by source, from any player reference on any screen (post-Figma).

- **Phase 4 — Analytics**. Not started, deferred until Phase 3 is real. New home (per ADR-009) for everything analytics-flavored that was originally scoped into Phase 2:
  - Standings, power rankings, in-season trends, matchup recaps, and week highlights — rebuilt fresh against whatever the Player Profile and Sleeper Experience look like by then, not resurrected from the code removed per ADR-009.
  - Multi-player comparison tool, built directly on `PlayerProfile` — no new aggregation logic needed.
  - True multi-season history, including a persisted `StandingSnapshot` entity (see ADR-006) once recomputing standings from raw matchup history on every read no longer scales.

- **Phase 5 — AI Assistant** (TBD). Not started, scope undefined. AI-assisted identity-match suggestions (Phase 3) are one possible candidate for this phase, not committed scope.

**Permanently out of scope / not phase-scheduled:** real-time/live-game sync, billing (only if monetization is revisited), CI/CD automation (GitHub Actions) — see `docs/ARCHITECTURE.md`, "What's out of scope for the current roadmap," and `docs/DECISIONS.md` ADR-002.
