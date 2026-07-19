# Architecture

> **Status: provisional.** This reflects the current scaffold only. Full architecture (data layer, auth, deployment, API design) is being defined during the planning phase — see `docs/DECISIONS.md` and `docs/ROADMAP.md`.

## Current shape

- **`LeagueLens.Server`** — ASP.NET Core 10 Web API. Minimal hosting-model startup (`Program.cs`): `AddControllers()`, `AddOpenApi()` (dev only), serves the Angular build output as static files, falls back to `index.html` for client-side routing.
- **`LeagueLens.Client`** — Angular 21 SPA, NgModule-based (not standalone components), built with `@angular/build`, tested with Vitest.

## Wiring

- **Development**: `LeagueLens.Server.csproj` references `LeagueLens.Client.esproj`; running the server also starts the Angular dev server, and `Microsoft.AspNetCore.SpaProxy` forwards non-API requests to it. The client's `proxy.conf.js` forwards API-path requests the other direction, to the backend.
- **Production**: the server serves the Angular production build (`dist/leaguelens.client/browser/`) directly as static files.

No data layer, authentication, or external integrations exist yet — those are architecture decisions still to be made.
