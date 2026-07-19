# Project Context

*Current state only. For durable guidance see `CLAUDE.md`; for decision history see `docs/DECISIONS.md`.*

**What it is:** LeagueLens — a fantasy football statistics/analytics web app, and a portfolio piece for demonstrating full-stack engineering skill. Started as a side project (co-developed with Mike) to explore fantasy football data while leveling up frontend skills.

**Stage:** Foundation just established. The repo was renamed from its original scaffold name (`SleeperApp`) to `LeagueLens`, the Visual Studio demo code (WeatherForecast) was removed, and baseline docs (`README.md`, `docs/`) were created. **No application features exist yet.** Architecture planning is the next step before any feature work begins.

**Stack (as scaffolded, not yet reconsidered):** ASP.NET Core 10 Web API (`LeagueLens.Server`) + Angular 21 SPA (`LeagueLens.Client`, NgModule-based), wired via SPA proxy in dev / static file serving in prod.

**Next:** see `docs/ROADMAP.md`.
