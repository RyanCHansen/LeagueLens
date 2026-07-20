# Project Context

*Current state only. For durable guidance see `CLAUDE.md`; for decision history see `docs/DECISIONS.md`.*

**What it is:** LeagueLens — a fantasy football statistics/analytics web app, and a portfolio piece for demonstrating full-stack engineering skill. Started as a side project to explore fantasy football data while leveling up cloud/full-stack skills. Its core value proposition is the **unified Player Profile** — one canonical view per player, compiling data pooled from every source (Sleeper, KeepTradeCut, FantasyCalc, FantasyPros, and more over time) into clearly sectioned, provenance-labeled data, reachable from any player reference anywhere in the app.

**Stage:** Foundation and architecture planning are both complete (see `docs/ARCHITECTURE.md`, `docs/DECISIONS.md`). **No application code exists yet.** Next step is Phase 2 — League Intel — see `docs/ROADMAP.md`.

**Note:** the renamed scaffold's client/server wiring (SPA proxy, single-deployable static file serving) is being replaced per the planned architecture — the API and SPA will deploy separately. Don't treat the current `Program.cs`/`proxy.conf.js` setup as the target design.

**Frontend:** a Figma design exists for the UI but hasn't been shared into the repo yet. No frontend implementation work starts until it is.

**Next:** see `docs/ROADMAP.md`.
