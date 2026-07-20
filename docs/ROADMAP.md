# Roadmap

- **Phase 1 — Foundation** ✅ Done. Repo renamed to LeagueLens, demo scaffold removed, baseline docs in place, architecture planning complete (see `docs/ARCHITECTURE.md` and `docs/DECISIONS.md`, ADR-001 through ADR-004).

- **Phase 2 — League Intel** ⏳ Next. Standings, power rankings, in-season trends, matchup previews/recaps, on Sleeper-only data. Built as a complete vertical slice, layer by layer:
  1. **Domain** — `User`, `League`, `LeagueMembership`, `Roster`, `Matchup`, `StandingSnapshot`, and `Player` (lightweight identity, bootstrapped from Sleeper's player list so `Roster` has something to reference) as persistence-ignorant POCOs.
  2. **Persistence** — EF Core `DbContext` + Fluent API configurations + migrations for the above.
  3. **Application** — concrete Sleeper sync module (no provider interface yet — single provider, nothing to abstract), standings/power-ranking/trend calculation services, structured logging throughout.
  4. **API** — endpoints for standings, power rankings, trends, matchup previews/recaps.
  5. **Infrastructure** — `docker-compose.yml` (API + local SQL Server), `scripts/` folder (setup, DB reset, dev data seeding, local startup), Azure deployment via Bicep (Static Web Apps + Container Apps + Container Apps Jobs + Azure SQL), manual deploy.
  6. **Frontend** — Angular UI, following a Figma design (not yet shared) once available; no frontend work starts before that.

- **Phase 3 — Unified Player Profiles**. The flagship feature: a canonical, compiled view of every player, pooled from every source and sectioned by provenance. Starts once Phase 2 ships; repeats the same six-layer sequence:
  1. **Domain** — `PlayerSourceRecord` (raw + normalized payload) as a persistence-ignorant POCO; common provider interface **extracted here**, once KeepTradeCut/FantasyCalc/FantasyPros join Sleeper as real second/third/fourth implementations (see ADR-004).
  2. **Persistence** — EF Core entities/migrations for `PlayerSourceRecord` via Fluent API.
  3. **Application** — `PlayerProfileService` (single + batch composition, cache-ready), per-provider sync modules (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros — each own auth/parsing/error-handling/rate-limiting behind the provider interface), identity resolution (resolved/low-confidence/unresolved states), a minimal/functional admin review interface for manual approve/reject/correct.
  4. **API** — `PlayerProfile` endpoints (single + batch).
  5. **Infrastructure** — scraper-specific deployment needs as they arise.
  6. **Frontend** — universal click-to-detail UI, sectioned by source, from any player reference on any screen (post-Figma).

- **Phase 4 — Analytics**. Not started, deferred until Phase 3 is real.
  - Multi-player comparison tool, built directly on `PlayerProfile` — no new aggregation logic needed.
  - True multi-season history.

- **Phase 5 — AI Assistant** (TBD). Not started, scope undefined. AI-assisted identity-match suggestions (Phase 3) are one possible candidate for this phase, not committed scope.

**Permanently out of scope / not phase-scheduled:** real-time/live-game sync, billing (only if monetization is revisited), CI/CD automation (GitHub Actions) — see `docs/ARCHITECTURE.md`, "What's out of scope for the current roadmap," and `docs/DECISIONS.md` ADR-002.
