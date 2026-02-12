# Architecture Overview

## Summary
Commonword is a modular monolith. The API host (`Commonword.Api`) is thin; modules own their endpoints, domain types, and persistence configuration. A single EF Core `AppDbContext` lives in Infrastructure and loads entity configurations from module assemblies.

## Modules
- Puzzles: import, daily selection, puzzle retrieval
- Solving: sessions, per-cell entries
- Telemetry: event capture

## Data Storage
PostgreSQL tables:
- `puzzles` (JSONB puzzle definition)
- `solve_sessions`
- `entries`
- `telemetry_events`

Puzzle definitions are stored as JSONB to keep schema flexible. Entries are per-cell rows keyed by `(session_id, row, col)`.

## Endpoints (MVP)
- `POST /puzzles/import`
- `GET /puzzles/{id}`
- `POST /puzzles/{id}/mark-daily`
- `GET /puzzles/today`
- `POST /sessions`
- `GET /sessions/{id}`
- `PUT /sessions/{id}/cells/{row}/{col}`
- `POST /telemetry/events`
- `GET /health`

## Extensibility Plan
- Add ipuz/puz import pipelines
- Background jobs for analytics or notification
- Outbox + message broker for async processing
- More robust auth/session tracking
