# LeagueLens

A companion application for Sleeper dynasty fantasy football leagues. Users manage their league in Sleeper; LeagueLens is where they explore unified player profiles that combine rankings, trade values, news, and analysis from multiple sources. An ASP.NET Core + Angular portfolio project.

## Stack

- **`LeagueLens.Server`** — ASP.NET Core 10 Web API
- **`LeagueLens.Client`** — Angular 21 SPA (NgModule-based, Vitest for tests)

## Running locally

Open `LeagueLens.slnx` in Visual Studio and run, or from the command line:

```bash
cd LeagueLens.Server
dotnet run
```

This builds and serves the Angular client via the SPA proxy in development. To run the client standalone:

```bash
cd LeagueLens.Client
npm install
npm start
```

## Docs

- [`PROJECT_CONTEXT.md`](./PROJECT_CONTEXT.md) — current project state
- [`docs/ARCHITECTURE.md`](./docs/ARCHITECTURE.md) — system architecture
- [`docs/ROADMAP.md`](./docs/ROADMAP.md) — what's next
- [`docs/DECISIONS.md`](./docs/DECISIONS.md) — architectural decision log
- [`CLAUDE.md`](./CLAUDE.md) — guidance for AI-assisted development in this repo

## License

MIT — see [`LICENSE`](./LICENSE).
