# Roadmap

- **Phase 0 — Foundation** ✅ Done. Repo renamed to LeagueLens, demo scaffold removed, baseline docs in place.
- **Phase 1 — Architecture planning** ✅ Done. See `docs/ARCHITECTURE.md` and `docs/DECISIONS.md` (ADR-002).
- **Phase 2 — v1 implementation** ⏳ Next. Not started.
  - Data model / EF Core schema (User, League, LeagueMembership, Roster, Matchup, StandingSnapshot)
  - Sleeper sync worker (periodic, hourly/daily)
  - API endpoints (standings, power rankings, in-season trends, matchup previews/recaps)
  - Local Docker dev environment (API + SQL via `docker-compose.yml`)
  - Angular frontend — implementation will follow a Figma design (not yet shared) once available; no frontend work starts before that
  - Azure deployment via Bicep (Static Web Apps + Container Apps + Container Apps Jobs + Azure SQL), manual deploy
- **Phase 3 — v2** Not started, deferred until v1 is real.
  - Trade/roster analysis via KeepTradeCut, FantasyCalc, FantasyPros scraping
  - True multi-season history
  - Real-time/live-game sync
  - CI/CD automation (GitHub Actions)
  - Billing (only if monetization is revisited)
