# AGENTS.md — Commonword

## Project overview
**Commonword** is a crossword puzzle solver with:
- **Backend:** ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL (local Docker) + Neon (prod).
- **Clients:**
    - **mobile-tauri:** Svelte + Vite + Tauri (Rust).
    - **telegram-miniapp:** Svelte + Vite static web app (Cloudflare Pages).
- **Core MVP:** import puzzle (JSON), “today’s puzzle” (most recent daily/most recent import), solve sessions (per-cell entries), telemetry capture.

## Repo map
- `backend/src/Commonword.Api` — HTTP host (thin), middleware, Swagger, CORS, health.
- `backend/src/Commonword.Infrastructure` — EF Core DbContext, migrations, cross-cutting infra.
- `backend/src/Commonword.Contracts` — request/response DTOs shared across modules.
- `backend/src/Modules/*` — feature modules: `Puzzles`, `Solving`, `Telemetry` (no cross-module references).
- `clients/mobile-tauri` — Tauri app.
- `clients/telegram-miniapp` — Telegram web app.
- `docs/` — architecture + decisions.

## Key rules (do not break)
1) **Module boundaries:** Modules must not reference each other directly. Communicate via IDs, shared Contracts, or events later.
2) **Api stays thin:** endpoints map to module application handlers; no domain logic in `Commonword.Api`.
3) **Central Package Management:** package versions live only in `backend/Directory.Packages.props`. No version attributes in `.csproj`.
4) **.NET 10:** all backend projects target `net10.0`.
5) **Small schema:** puzzle definition stored as **JSONB** in `puzzles.data`. Normalize later only if needed.

## Conventions (backend)
- **Naming**
    - Projects: `Commonword.*`
    - Namespaces mirror folders: `Commonword.Modules.Puzzles.*`
    - Endpoints: `*Endpoints.cs` per module.
    - Handlers: `XCommand`, `XQuery`, `XHandler`.
- **API style**
    - Minimal APIs preferred.
    - Use `Results<...>` or `TypedResults` where sensible.
    - Return `ProblemDetails` for validation / errors (avoid throwing for expected failures).
- **Validation**
    - Validate requests at boundary (endpoint) or in application handlers.
    - Keep validation errors consistent (400 with details).
- **Persistence**
    - EF Core migrations live in `Commonword.Infrastructure/Persistence/Migrations`.
    - `AppDbContext` lives in Infrastructure.
    - Module entity configurations live in each module under `Persistence/` and are registered by Infrastructure at startup.
- **Time**
    - Use `DateTimeOffset.UtcNow` (or an injected clock service) for timestamps.

## Conventions (frontend)
- Use `VITE_API_BASE_URL` for API base.
- Keep clients “thin”: fetch + render; avoid duplicating backend rules.
- Prefer simple stores + components; no premature abstractions.

## Package managers / tooling
- **Backend:** `dotnet` CLI.
- **Frontend (Svelte/Vite):** use `npm` (or pnpm) as needed by scaffolding.
- **Tauri / Rust:** use **cargo** for Rust side:
    - Build/check: `cargo build`, `cargo test` in `clients/mobile-tauri/src-tauri`
    - Avoid introducing nonessential Rust crates.

> Note: Tauri still uses Node tooling for the web frontend bundling; “cargo instead of npm” refers to Rust-side tasks in `src-tauri/`.

## Environment variables
### Backend
- `ConnectionStrings__Default`
    - Local example: `Host=localhost;Port=5432;Database=commonword;Username=commonword;Password=commonword`
    - Neon typically requires SSL (documented in README).

### Clients
- `VITE_API_BASE_URL` — e.g. `http://localhost:5080` for dev, prod API URL for deployments.

## Local development (quick)
1) Start Postgres:
    - `docker compose up -d` (repo root)
2) Backend:
    - `cd backend`
    - `dotnet restore`
    - `dotnet ef database update -p src/Commonword.Infrastructure -s src/Commonword.Api`
    - `dotnet run --project src/Commonword.Api`
3) Clients:
    - `clients/telegram-miniapp`: run dev server per README
    - `clients/mobile-tauri`: run dev per README (web + Tauri)

## Deployment notes
- **API (DigitalOcean):** set `ConnectionStrings__Default` to Neon connection string; ensure SSL options.
- **Telegram web (Cloudflare Pages):** build static site; set `VITE_API_BASE_URL` in build env.

## Current endpoints (MVP)
- `POST /puzzles/import`
- `GET /puzzles/{id}`
- `POST /puzzles/{id}/mark-daily`
- `GET /puzzles/today`
- `POST /sessions`
- `GET /sessions/{id}`
- `PUT /sessions/{id}/cells/{row}/{col}`
- `POST /telemetry/events`
- `GET /health`

## Styling / formatting
- C#:
    - Nullable enabled, implicit usings enabled.
    - Use `file-scoped namespaces`.
    - Favor small files; keep types close to feature.
- TS/Svelte:
    - Use ESLint/Prettier defaults from scaffold.
    - Prefer readable component structure over cleverness.

## Future extensions
- File import for `.ipuz`/`.puz`
- Collaboration (SignalR/WebSockets)
- Outbox + RabbitMQ/Kafka streaming
- Auth (Telegram identity mapping)

## References
- docs: `docs/architecture.md`
- DB schema and DTO shapes must remain backward compatible once clients depend on them.
