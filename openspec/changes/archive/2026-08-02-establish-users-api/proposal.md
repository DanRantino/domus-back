## Why

Domus needs a backend identity boundary before any household domain work: validate Logto-issued access tokens, represent a Domus User linked to the OIDC subject, and prove that login alone does not grant product access. The Domus Web already authenticates via Logto and calls `GET /me` expecting `401` / `403` / `200`; this change establishes the API that fulfills that contract and adds explicit self-serve provisioning.

## What Changes

- Introduce a greenfield .NET API host (local port `3001`) with PostgreSQL persistence on Railway.
- Validate Bearer JWT access tokens against the Logto OIDC issuer (signature, issuer, audience, expiration).
- Introduce the `users` capability: Domus User with Domus `id` and `identity_id` (= OIDC `sub`); no credentials; no Domus profile fields.
- Expose `GET /me` that resolves the caller without side-effect provisioning (`401` / `403` / `200`).
- Expose `POST /me` for explicit self-serve provisioning from the authenticated token `sub` (`201` / `409`).
- Explicitly exclude Houses, membership, roles, profile sync, and other domain capabilities.

## Capabilities

### New Capabilities
- `users`: Domus User lifecycle for identity integration — link to OIDC `sub`, no auto-provision on login or read, resolve authenticated identity to at most one User, current-user read (`GET /me`), and explicit self-serve creation (`POST /me`).

### Modified Capabilities
- _(none)_

## Impact

- **This repository (`back`)**: new ASP.NET Core API, EF Core + Npgsql, JWT Bearer auth, Users persistence and `/me` endpoints, env-based configuration, contract tests.
- **PostgreSQL (Railway)**: `users` table; connection string via environment.
- **Logto preprod**: API resource / audience must align with API JWT validation and the frontend `VITE_LOGTO_API_RESOURCE`.
- **Frontend repository (`front`)**: already depends on `GET /me`; will need a follow-up change to call `POST /me` from the unprovisioned state (out of scope for implementation here).
- **Out of scope**: Houses, Members, roles, Domus profile fields, Logto Management API, auto-provision on login.
