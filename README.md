# SprintLabs backend

.NET 8 education/game API using ASP.NET Core controllers, MediatR, EF Core and MySQL.

Start with [AGENTS.md](AGENTS.md), then [current project context](docs/ai/current-state.md) and the [game developer backend guide](docs/ai/game-developer-guide.md).

Use the [backend + Unity starter prompt](docs/ai/codex-prompts.md) for cross-repository work. API and frontend contracts live under `specs/<feature>/`. The combined B2C setup/shop integration guide is [here](specs/030-coin-shop/game-integration.md).

```powershell
dotnet restore SprintLabs.sln
dotnet build SprintLabs.sln
dotnet test SprintLabs.sln --no-restore
```

Runtime needs MySQL and private configuration; follow the developer guide before starting the API. Startup can apply migrations to the configured database. Pushes to `development` automatically deploy staging through GitHub Actions.
