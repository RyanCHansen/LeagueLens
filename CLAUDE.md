# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

For current project state (what's built, what's next), see `PROJECT_CONTEXT.md`. For architecture, see `docs/ARCHITECTURE.md`. For decision history, see `docs/DECISIONS.md`.

## Project shape

LeagueLens is a two-project solution generated from the Visual Studio "Angular + ASP.NET Core" SPA template:

- **`LeagueLens.Server`** — ASP.NET Core 10 (net10.0) Web API. Serves the built Angular app as static files and exposes API controllers.
- **`LeagueLens.Client`** — Angular 21 client (Angular CLI, standalone components + signals, no `NgModule`s — switched from the scaffold's original NgModule convention per ADR-012), built with the `@angular/build` (esbuild/Vite-based) builder and tested with Vitest. Styled with Tailwind CSS (v4, CSS-first config via `src/styles.css` + `.postcssrc.json` — no `tailwind.config.js`).

The two are wired together via `LeagueLens.slnx` (a `.slnx` solution file, not the classic `.sln` format) and an `esproj` reference: `LeagueLens.Server.csproj` references `LeagueLens.Client.esproj` so that running the server project also builds/serves the client (via `Microsoft.AspNetCore.SpaProxy` in development).

## Commands

### Client (`LeagueLens.Client/`)

Run from the `LeagueLens.Client` directory:

- `npm start` — dev server via `run-script-os` (dispatches to `start:windows` or `start:default`), which runs `ng serve` with HTTPS dev certs. Requires local ASP.NET dev cert (`node aspnetcore-https` runs automatically as `prestart`).
- `ng build` — production build, output to `dist/leaguelens.client/browser/`.
- `ng build --watch --configuration development` — dev build in watch mode.
- `ng test` — run the unit test suite with Vitest (via the `@angular/build:unit-test` builder — despite `karma.conf.js` still existing in the repo, Karma is not the active runner).
- `ng generate component <name>` — scaffold a new component. Schematics default to **standalone** components with SCSS styles and `OnPush` change detection (`angular.json`'s `@schematics/angular:component` config) — match this convention when hand-writing new components.

### Server (`LeagueLens.Server/`)

Run from the `LeagueLens.Server` directory, or open `LeagueLens.slnx` in Visual Studio:

- `dotnet run` — runs the API; in development, the SPA proxy (`SpaProxyServerUrl` in `.csproj`, `https://localhost:53485`) forwards to the Angular dev server so a single F5/`dotnet run` serves both.
- `dotnet build` — build the server project (and, via the project reference, the client).
- `dotnet test` — no test project exists yet in the solution.

There is no repo-wide lint/format command configured yet; Prettier config exists for the client (`.prettierrc`: single quotes, 100 print width, Angular parser for `*.html`) but isn't wired to an npm script.

## Architecture conventions

- **API/client contract**: the client proxies API paths to the backend in dev via `LeagueLens.Client/src/proxy.conf.js`. **When adding a new controller/route, add its path to the `context` array in `proxy.conf.js`** or client requests to it will 404 against the dev server instead of reaching the API.
- **Client bootstrap**: standalone, via `bootstrapApplication(AppComponent, appConfig)` in `main.ts`. `app.config.ts` holds the `ApplicationConfig` (router, HTTP client, error listeners); there is no `AppModule`. New components declare their own `imports: [...]` rather than being declared in a module.
- **Client folders**: `src/app/layout/` holds shell/chrome components (`shell` composes `nav-rail` + `topbar` + `<router-outlet>`), `src/app/pages/` holds routed page components (e.g. `dashboard`), `src/app/shared/` holds small reusable presentational components used across features (e.g. `icon`), `src/app/core/` holds app-wide singleton state services (e.g. `league-context`) as distinct from `shared/`'s presentational components, `src/app/Models/` holds TypeScript interfaces mirroring server DTOs. Follow this split when adding new features rather than flattening everything into `src/app/`.
- **Routing**: routes are declared in `app.routes.ts` as a plain `Routes` array, provided via `provideRouter(routes)` in `app.config.ts`.
- **Server startup** (`Program.cs`) is minimal hosting-model style: `AddControllers()` + `AddOpenApi()`, `UseDefaultFiles()`/`MapStaticAssets()` to serve the Angular build output, `MapOpenApi()` gated to `Development`, and `MapFallbackToFile("/index.html")` for client-side routing support in production.
- **Domain/Persistence separation:** domain entities are persistence-ignorant POCOs — no EF Core attributes or base classes. Table mapping, keys, relationships, and constraints are configured in the Persistence layer via Fluent API (`IEntityTypeConfiguration<T>`), never via data annotations on domain classes.
- **Structured logging** (`ILogger<T>`, structured message templates, not string concatenation) is used from the first milestone, not retrofitted later.
- **`CancellationToken`** propagates through async operations where appropriate, per modern ASP.NET Core practice.
- See `docs/ARCHITECTURE.md` ("Provider Abstraction", "Player Profile") and `docs/DECISIONS.md` (ADR-003, ADR-004) for the Player domain model and provider-sync design.

## Working conventions

- Code is the source of truth. Keep markdown docs concise and update them only when something durable actually changes — don't let docs drift into a second copy of the code.
- Architecture planning is complete (see `docs/ARCHITECTURE.md`, `docs/DECISIONS.md` ADR-001 through ADR-004). No application code exists yet. Before building anything, check those docs — don't assume the current scaffold's client/server wiring (SPA proxy, single-deployable static file serving) is the target design; it's being replaced.
- **Implementation proceeds in small, reviewable milestones**, following the roadmap in `docs/ROADMAP.md` (Foundation → League Intel → Unified Player Profiles → Analytics → AI Assistant, each of League Intel and Unified Player Profiles broken into Domain → Persistence → Application → API → Infrastructure → Frontend layers). For each milestone: explain the goal and which files will change, wait for approval, implement only that milestone, summarize the completed work, recommend the next milestone, then stop and wait for review. If scoping reveals a milestone has grown too large, split it before implementation begins, not mid-flight. This project is meant to demonstrate thoughtful engineering decisions, not rapid feature output.
- **Before answering, state what information is needed to answer well.**
