# Commonword

Commonword is a modular monolith backend with lightweight clients for solving crossword-style puzzles. The MVP supports importing puzzles as JSON, daily selection, solving sessions, per-cell entry updates, and telemetry capture.

## Tech Stack
- Backend: ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL
- Clients: Svelte + Vite (web) and Svelte + Vite + Tauri (desktop/mobile shell)
- Hosting: Neon Postgres for production, Cloudflare Pages for web clients

## Repo Layout
- `backend/` .NET solution, modules, contracts, and infrastructure
- `clients/packages/ui-core/` Shared Svelte UI package
- `clients/apps/web/` Svelte + Vite web UI (Telegram mini-app)
- `clients/apps/tauri/` Svelte + Vite + Tauri UI
- `docs/` architecture and ADRs

## Local Dev

### 1) Start Postgres
```bash
docker compose up -d
```

### 2) Run the API
```bash
cd backend/src/Commonword.Api
# Local dev uses docker-compose Postgres
# Connection string is in appsettings.json

dotnet run
```

API base URL (default): `http://localhost:5000`

Swagger: `http://localhost:5000/swagger`
Health: `http://localhost:5000/health`

### 3) Run the clients

Mobile (Tauri shell + Svelte dev server):
```bash
cd clients
pnpm install
pnpm -C apps/tauri tauri dev
```

Telegram miniapp (web):
```bash
cd clients
pnpm -C apps/web dev
```

Both clients read `VITE_API_BASE_URL`. Example `.env`:
```
VITE_API_BASE_URL=http://localhost:5000
```

## Configuration

### Connection strings
- Local: `appsettings.json` uses docker Postgres.
- Production: set `ConnectionStrings__Default` env var (Neon). Example:
```
ConnectionStrings__Default=Host=ep-example.neon.tech;Database=commonword;Username=commonword;Password=...;Ssl Mode=Require;Trust Server Certificate=true
```

### CORS
`Cors:AllowedOrigins` lives in `backend/src/Commonword.Api/appsettings.json`. Add your client origins there.

## Migrations
Migrations live in `backend/src/Commonword.Infrastructure/Persistence/Migrations`.

Apply migrations:
```bash

dotnet tool restore
dotnet ef database update -p src/Commonword.Infrastructure -s src/Commonword.Api

```

Add a new migration:
```bash
cd backend

dotnet ef migrations add <Name> \
  --project src/Commonword.Infrastructure \
  --startup-project src/Commonword.Api \
  --output-dir Persistence/Migrations
  
dotnet ef database update -p src/Commonword.Infrastructure -s src/Commonword.Api
```

## API Endpoints (MVP)
- `POST /puzzles/import`
- `GET /puzzles/{id}`
- `POST /puzzles/{id}/mark-daily`
- `GET /puzzles/today`
- `POST /sessions`
- `GET /sessions/{id}`
- `PUT /sessions/{id}/cells/{row}/{col}`
- `POST /telemetry/events`
- `GET /health`

example:
import a puzzle:
```curl -s -X POST "http://localhost:5000/puzzles/import" \
  -H "Content-Type: application/json" \
  -d '{
    "title":"EF Test Puzzle",
    "puzzleData":{
      "width":5,
      "height":5,
      "grid":[".....","..#..",".....","..#..","....."],
      "clues":{"across":[{"number":1,"text":"Test across"}],"down":[{"number":1,"text":"Test down"}]}
    }
  }'
  ```

using buildx for multi-arch:
```  docker buildx build \
    --platform linux/amd64,linux/arm64 \
    -t ghcr.io/<your-github-username>/commonword-api:latest \
    --push .    
```
## Notes
- Puzzle definitions are stored as JSONB in Postgres.
- The daily puzzle endpoint returns the most recent `is_daily=true` puzzle, otherwise the most recent import.
- Marking a puzzle as daily clears `is_daily` on other puzzles to keep a single daily.
