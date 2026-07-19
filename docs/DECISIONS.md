# Decisions

Lightweight architecture decision log. One entry per significant decision: what was decided, why, and what else was considered.

---

## ADR-001: Establish LeagueLens as a renamed, cleaned foundation

**Date:** 2026-07-18
**Status:** Accepted

**Decision:** Renamed the repo/solution from the Visual Studio scaffold name `SleeperApp` to `LeagueLens` (projects, namespaces, GitHub repo, docs), removed the WeatherForecast demo code, and added a minimal docs structure (`README.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/DECISIONS.md`, `PROJECT_CONTEXT.md`).

**Why:** LeagueLens is intended as a portfolio flagship project and needed a clean, professionally-named foundation before any real feature work begins, rather than building on top of scaffold-generated names and demo code.

**Alternatives considered:** Keeping the `SleeperApp` name and renaming later — rejected, since every additional commit under the old name is more to rewrite later for no benefit.
