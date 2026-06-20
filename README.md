# Rytmix API

The ASP.NET Core (C#) backend for **Rytmix**, a web-first music streaming app. It serves
the web frontend and (later) a Tauri desktop client over HTTPS, proxies the Jamendo music
API, and persists user data (accounts, playlists, favorites) in PostgreSQL via EF Core.

This is one of two repositories:
- **`rytmix-api`** (this repo) — ASP.NET Core backend, deployed to Render via Docker.
- **`rytmix-web`** — Next.js frontend (+ Tauri desktop wrapper), deployed to Cloudflare Pages.

## Tech stack

| Concern | Choice |
|---|---|
| Framework | ASP.NET Core Web API (.NET 10, LTS) |
| Language | C# |
| Data access | EF Core with the Npgsql provider |
| Database | PostgreSQL (Supabase) |
| Auth *(planned)* | ASP.NET Core Identity + JWT |
| Containerization | Docker (multi-stage build) |
| Hosting | Render (free tier) |

## Project structure

```
rytmix-api/
├── Controllers/          # HTTP endpoints (planned; none yet)
├── Models/               # Entities + DTOs (planned; none yet)
├── Data/
│   └── AppDbContext.cs   # EF Core DbContext (Npgsql); no entities yet
├── Program.cs            # DI + middleware: controllers, /health, CORS, DbContext
├── appsettings.json      # Non-secret config (connection string is empty here)
├── Dockerfile            # Multi-stage build; binds to Render's $PORT
├── .github/workflows/    # CI: dotnet build/test on PRs
└── rytmix-api.csproj
```

## Getting started (local)

**Prerequisites:** the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# from the repo root
dotnet restore
dotnet run
```

The API starts on `http://localhost:5139`. Verify it's up:

```bash
curl http://localhost:5139/health
# -> {"status":"healthy"}
```

No database is required to run locally — `/health` does not touch the DB, and nothing
resolves the `DbContext` until the data-layer work (accounts/persistence) is added.

## Configuration

Configuration is read from `appsettings.json`, overlaid by `appsettings.Development.json`
locally and by **environment variables** in production. Nested keys map to env vars with a
double underscore (`ConnectionStrings:DefaultConnection` → `ConnectionStrings__DefaultConnection`).

| Key | Env var | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Npgsql connection string for Postgres |
| `Jamendo:ClientId` | `Jamendo__ClientId` | Jamendo API client ID (proxied server-side, *planned*) |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0`, … | Allowed frontend origins (array) |

> **Never commit secrets.** Keep connection strings and API keys in `appsettings.Development.json`
> (gitignored) or user-secrets locally, and in Render environment variables in production.

## Running with Docker

```bash
docker build -t rytmix-api .
docker run -p 8080:8080 rytmix-api
# -> http://localhost:8080/health
```

The container binds Kestrel to `0.0.0.0:$PORT`, falling back to `8080` when `PORT` is unset.
Render injects `PORT` at runtime; binding to it is required for Render to route traffic.

## Deployment

Render builds the `Dockerfile` and deploys automatically. Notable settings:
- **Health check path:** `/health` — also pinged every ~5 min by a cron-job.org job to keep
  the free instance warm (it spins down after 15 min idle).
- **Auto-deploy:** after CI checks pass on `main`.
- **Secrets:** set as Render environment variables (see the Configuration table above).

Production base URL: `https://rytmix-api.onrender.com`

## Build & test

```bash
dotnet build      # must pass before a task is "done"
dotnet test       # no test projects yet; added alongside features
```

CI ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)) runs `dotnet build`/`test` on
every PR into `main` and gates Render deploys.

## API endpoints

| Method | Path | Status |
|---|---|---|
| GET | `/health` | ✅ Live |
| GET | `/api/tracks/search?q=...` | 🔜 Planned (Jamendo proxy) |
| POST | `/api/auth/register`, `/api/auth/login` | 🔜 Planned |
| — | `/api/playlists`, `/api/favorites` | 🔜 Planned (JWT-protected) |

## Contributing

- Branch from `main` (e.g. `feature/<name>`); open a **Pull Request** — never push to `main`
  directly. The PR author should not be the approver.
- Keep controllers thin → logic in services → services use EF Core.
- `async`/`await` for all I/O; DTOs for request/response (never expose EF entities directly).
- `dotnet build` (and tests, once they exist) must pass before merging.

## License

© 2026 the Rytmix team. Published for portfolio/demonstration purposes — please don't reuse
without permission.
