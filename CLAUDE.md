# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

For current project state (what's built, what's next), see `PROJECT_CONTEXT.md`. For architecture, see `docs/ARCHITECTURE.md`. For decision history, see `docs/DECISIONS.md`.

## Project shape

LeagueLens is a two-project solution generated from the Visual Studio "Angular + ASP.NET Core" SPA template:

- **`LeagueLens.Server`** — ASP.NET Core 10 (net10.0) Web API. Serves the built Angular app as static files and exposes API controllers.
- **`LeagueLens.Client`** — Angular 21 client (Angular CLI, non-standalone/NgModule-based components), built with the `@angular/build` (esbuild/Vite-based) builder and tested with Vitest.

The two are wired together via `LeagueLens.slnx` (a `.slnx` solution file, not the classic `.sln` format) and an `esproj` reference: `LeagueLens.Server.csproj` references `LeagueLens.Client.esproj` so that running the server project also builds/serves the client (via `Microsoft.AspNetCore.SpaProxy` in development).

## Commands

### Client (`LeagueLens.Client/`)

Run from the `LeagueLens.Client` directory:

- `npm start` — dev server via `run-script-os` (dispatches to `start:windows` or `start:default`), which runs `ng serve` with HTTPS dev certs. Requires local ASP.NET dev cert (`node aspnetcore-https` runs automatically as `prestart`).
- `ng build` — production build, output to `dist/leaguelens.client/browser/`.
- `ng build --watch --configuration development` — dev build in watch mode.
- `ng test` — run the unit test suite with Vitest (via the `@angular/build:unit-test` builder — despite `karma.conf.js` still existing in the repo, Karma is not the active runner).
- `ng generate component <name>` — scaffold a new component. Schematics default to **non-standalone** components with SCSS styles (`angular.json` sets `standalone: false` for components/directives/pipes) — match this convention when hand-writing new components.

### Server (`LeagueLens.Server/`)

Run from the `LeagueLens.Server` directory, or open `LeagueLens.slnx` in Visual Studio:

- `dotnet run` — runs the API; in development, the SPA proxy (`SpaProxyServerUrl` in `.csproj`, `https://localhost:53485`) forwards to the Angular dev server so a single F5/`dotnet run` serves both.
- `dotnet build` — build the server project (and, via the project reference, the client).
- `dotnet test` — no test project exists yet in the solution.

There is no repo-wide lint/format command configured yet; Prettier config exists for the client (`.prettierrc`: single quotes, 100 print width, Angular parser for `*.html`) but isn't wired to an npm script.

## Architecture conventions

- **API/client contract**: the client proxies API paths to the backend in dev via `LeagueLens.Client/src/proxy.conf.js`. **When adding a new controller/route, add its path to the `context` array in `proxy.conf.js`** or client requests to it will 404 against the dev server instead of reaching the API.
- **Client module structure**: NgModule-based (not standalone components). `app-module.ts` declares/bootstraps `AppComponent` and imports `AppRoutingModule`; new components must be declared in a module (either `app-module.ts` or a feature module) to be usable.
- **Client folders**: `src/app/layout/` holds shell/chrome components (e.g. `header`), `src/app/pages/` holds routed page components (e.g. `home`), `src/app/Models/` holds TypeScript interfaces mirroring server DTOs. Follow this split when adding new features rather than flattening everything into `src/app/`.
- **Routing**: routes are declared in `app-routing-module.ts` against `Routes`/`RouterModule.forRoot`.
- **Server startup** (`Program.cs`) is minimal hosting-model style: `AddControllers()` + `AddOpenApi()`, `UseDefaultFiles()`/`MapStaticAssets()` to serve the Angular build output, `MapOpenApi()` gated to `Development`, and `MapFallbackToFile("/index.html")` for client-side routing support in production.

## Working conventions

- Code is the source of truth. Keep markdown docs concise and update them only when something durable actually changes — don't let docs drift into a second copy of the code.
- Don't implement application features without an agreed architecture (see `docs/ARCHITECTURE.md` and `docs/DECISIONS.md`) — this project is in an explicit planning phase until those are filled in.
