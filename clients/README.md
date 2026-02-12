# Commonword Clients

## Start Backend

From the repo root:

```bash
docker compose up -d
cd backend
dotnet run --project src/Commonword.Api
```

## Setup

Install dependencies from the clients workspace root:

```bash
cd clients
pnpm install
```

Set API base URL (examples):

```bash
cp apps/web/.env.example apps/web/.env
cp apps/tauri/.env.example apps/tauri/.env
```

## Web (Cloudflare Pages target)

```bash
pnpm -C apps/web dev
```

Build:

```bash
pnpm -C apps/web build
```

## Tauri

Run the Tauri shell with Vite:

```bash
pnpm -C apps/tauri tauri dev
```

Rust-side commands:

```bash
cd apps/tauri/src-tauri
cargo build
cargo fmt
```
