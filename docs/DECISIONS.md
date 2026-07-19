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

**Why:** This is a solo-developed (Mike is a non-technical collaborator) portfolio flagship project built entirely on free-tier infrastructure. Every decision above was chosen to balance three goals: keep v1 shippable and scoped, demonstrate real engineering judgment (data modeling, cloud-agnostic design, containerization, testing discipline) rather than just feature output, and stay within genuinely-free hosting (not trial credits) so the deployed app remains available indefinitely for portfolio review.

**Alternatives considered:**
- Per-user private league data instead of a shared `League` entity — rejected as wasteful (the same real league's data would be synced and stored redundantly for every member who signs up) and a poor fit for the domain (members of the same league should see identical numbers).
- Fully gated access (login required for everything) — rejected in favor of public read-only pages, since a reviewer being able to see real output without signing up matters for a portfolio piece.
- Near-real-time/live-game sync — rejected for v1 as incompatible with the free compute budget and unnecessary for the standings/trends/recap feature set chosen.
- Terraform over Bicep — rejected since the project is Azure-only today and Bicep has less boilerplate; IaC is deployment-target-specific by nature regardless of tool, so this doesn't compromise the cloud-agnostic application architecture.
- Trade/roster analysis in v1 — rejected despite being the reason KeepTradeCut/FantasyCalc scraping is planned, because it depends on a separate, fragile scraping pipeline that shouldn't block shipping the core sync/analytics pipeline first.
