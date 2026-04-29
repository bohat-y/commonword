# Commonword

Commonword is a modular monolith backend with lightweight clients for solving crossword-style puzzles. The MVP supports importing puzzles as JSON, daily selection, solving sessions, per-cell entry updates, and telemetry capture.

## Tech Stack
- Backend: ASP.NET Core Web API (.NET 10), EF Core, PostgreSQL
- Clients: Android native (Kotlin + Jetpack Compose), Svelte + Vite web, and Svelte + Vite + Tauri
- Hosting: Neon Postgres for production, DigitalOcean App Platform for the API, Cloudflare Pages for web clients

## Repo Layout
- `backend/` .NET solution, modules, contracts, and infrastructure
- `clients/packages/ui-core/` Shared Svelte UI package
- `clients/apps/web/` Svelte + Vite web UI (Telegram mini-app)
- `clients/apps/tauri/` Svelte + Vite + Tauri UI
- `clients/apps/android-native/` Native Android app using Kotlin, Jetpack Compose, Retrofit, and DataStore
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

Android native app:

Open Android Studio with the native app:
```bash
studio clients/apps/android-native
```

Use **Device Manager** in Android Studio to create and start an emulator, then run the app directly from the IDE. Or, with an emulator already running, install via Gradle:

```bash
cd clients/apps/android-native
./gradlew assembleDebug
``` The Android app uses Retrofit to call the Commonword API and stores local player/session metadata with DataStore.

Android API base URL defaults:
- `debug`: deployed API by default, override with `COMMONWORD_DEBUG_API_BASE_URL`
- `deviceDebug`: deployed API by default, override with `COMMONWORD_DEVICE_API_BASE_URL`
- `release`: deployed API by default, override with `COMMONWORD_PROD_API_BASE_URL`

For a physical device calling a local backend, use your machine's LAN IP rather than `localhost`:
```bash
./gradlew installDeviceDebug -PCOMMONWORD_DEVICE_API_BASE_URL=http://192.168.1.20:5000/
```

For an emulator calling a local backend, use:
```bash
./gradlew installDebug -PCOMMONWORD_DEBUG_API_BASE_URL=http://10.0.2.2:5000/
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

### Android signing
CircleCI can build signed Android release artifacts. Signing secrets are stored in the `commonword-android-release` context:
- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEY_PASSWORD`

The keystore file itself must not be committed. Local keystore files are ignored by `.gitignore`.

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

## Android Native Client

The native Android app is in `clients/apps/android-native`.

Key features:
- loads the daily puzzle from the API
- starts or resumes a solving session
- renders an interactive crossword grid
- syncs cell entries to the API
- checks completed words through the API
- supports local player/session persistence with DataStore
- includes English and Spanish string resources

Useful commands:
```bash
cd clients/apps/android-native

# Local JVM tests
./gradlew testDebugUnitTest

# Instrumented Compose UI tests on a connected device/emulator
./gradlew connectedDebugAndroidTest

# Lint and JaCoCo reports
./gradlew lintDebug jacocoTestReport

# Release artifacts, signed when signing env vars are present
./gradlew assembleRelease bundleRelease
```

Localization resources:
- English: `app/src/main/res/values/strings.xml`
- Spanish: `app/src/main/res/values-es/strings.xml`

CI behavior:
- `android-quality` runs local JVM Android tests such as `HomeViewModelTest`.
- `android-reports` generates Android lint and JaCoCo artifacts.
- `android-release` builds signed APK/AAB artifacts, verifies their signatures, and stores them as CircleCI artifacts.
- Instrumented Compose UI tests under `app/src/androidTest` are not currently run in CircleCI; run them with `connectedDebugAndroidTest`.

## CI/CD

CircleCI runs separate API and client workflows.

API workflow:
- restores, builds, and tests the ASP.NET Core solution
- runs a NuGet vulnerability audit
- on `main`, builds and pushes the API Docker image to GHCR using both `sha-<commit>` and `latest` tags
- on `main`, deploys the new API image to DigitalOcean App Platform

Client workflows:
- builds the Svelte web and Tauri shells
- runs JS dependency audit
- runs Android JVM unit tests
- produces frontend, Android lint, JaCoCo, and signed Android release artifacts on `main`

## Notes
- Puzzle definitions are stored as JSONB in Postgres.
- The daily puzzle endpoint returns the most recent `is_daily=true` puzzle, otherwise the most recent import.
- Marking a puzzle as daily clears `is_daily` on other puzzles to keep a single daily.

