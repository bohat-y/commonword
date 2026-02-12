# 0001 - Modular Monolith + JSONB

Date: 2026-02-05

## Context
We need a fast MVP that supports import, daily puzzle selection, solving sessions, and telemetry. We also need clean module boundaries and easy extensibility without premature microservices.

## Decision
- Use a modular monolith architecture.
- Keep the API host thin; modules own endpoints and domain logic.
- Store puzzle definitions in JSONB for flexibility.

## Consequences
- A single database and `AppDbContext` keeps local dev simple.
- JSONB gives schema flexibility for varying puzzle formats.
- Future externalization is possible by carving modules into services later.
