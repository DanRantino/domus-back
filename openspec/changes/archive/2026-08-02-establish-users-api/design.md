## Context

See proposal.md for motivation. This repository is a greenfield Domus API (`back`) with OpenSpec scaffolding and backend/architecture rules; there is no application code yet. Authentication is delegated to Logto at `https://logto-auth-preprod.up.railway.app/` (OIDC issuer `.../oidc`). The Domus Web (`front`) already obtains Logto access tokens and calls `GET /me` expecting `{ id, identity_id }` with status `401` / `403` / `200`. PostgreSQL is hosted on Railway. Architecture requires OIDC concepts over Logto-specific APIs where practical, `identity_id` = OIDC `sub`, and no automatic Domus access from IdP authentication alone.

## Goals / Non-Goals

**Goals:**

- Minimal .NET API host that validates Logto JWTs and persists Domus Users in PostgreSQL.
- Fulfill `GET /me` contract used by the frontend.
- Add explicit self-serve `POST /me` that creates a User from the authenticated `sub`.
- Keep the User model limited to `id` + `identity_id`.

**Non-Goals:**

- Houses, membership, roles, or household authorization.
- Domus-owned profile fields (name, email, etc.).
- Operator-only / shared-secret internal provisioning as the primary path (self-serve is the chosen model).
- Auto-provision on login, callback, or `GET /me`.
- Multi-project Clean Architecture scaffolding beyond a single vertical host.
- API version prefix (`/v1`) unless the frontend changes with it.

## Decisions

### D1: Single-project vertical host
**Choice:** One ASP.NET Core project (`Domus.Api`) with a `Features/Users` vertical slice (endpoints, User model, DbContext).  
**Why:** Boundaries of responsibility without premature project sprawl; only one capability exists.  
**Alternatives:** Api + Core/Infrastructure multi-project split — deferred until cross-capability friction appears.

### D2: Minimal APIs for `/me`
**Choice:** Map `GET /me` and `POST /me` with Minimal APIs (or equivalent endpoint mapping), not a heavy controller layer.  
**Why:** Two endpoints; keep HTTP mapping thin and explicit.  
**Alternatives:** MVC Controllers — unnecessary ceremony for this surface.

### D3: JWT Bearer validation against Logto
**Choice:** `Microsoft.AspNetCore.Authentication.JwtBearer` with Authority/issuer = Logto OIDC issuer, Audience = configured API resource, JWKS from metadata.  
**Why:** Standard OIDC validation; no per-request Logto Management/API calls.  
**Alternatives:** Custom JWT handler or Logto SDK — rejected; adds coupling without benefit.

### D4: Self-serve provisioning from token `sub` only
**Choice:** `POST /me` creates `identity_id` exclusively from the authenticated token `sub`. No client-supplied `identity_id` in the body for this milestone. Duplicate → HTTP `409`. Success → HTTP `201` with `{ id, identity_id }`.  
**Why:** Explicit action (login still does not create Users) while preventing impersonation via body.  
**Alternatives:** Operator-only internal endpoint — weaker for SPA onboarding; auto-provision on `GET /me` — violates authenticated ≠ provisioned.

### D5: Minimal User persistence
**Choice:** Table `users` with `id` (UUID PK) and `identity_id` (text, unique, not null). EF Core + Npgsql + versioned migration. Connection via env (`DATABASE_URL` or `ConnectionStrings__Database`), SSL as required by Railway.  
**Why:** Matches observable contract; unique constraint enforces one User per identity.  
**Alternatives:** Cache email/name — deferred until a Domus domain need exists.

### D6: JSON snake_case for User responses
**Choice:** Response bodies use `id` and `identity_id` (snake_case) to match the existing frontend Zod schema.  
**Why:** Cross-repo contract already shipped in `front`.  
**Alternatives:** camelCase + frontend change — avoid dual churn in this milestone.

### D7: Local listen port 3001 and CORS for SPA
**Choice:** Dev URLs listen on `http://localhost:3001`; CORS allows the SPA origin (`http://localhost:3000`).  
**Why:** Aligns with frontend `VITE_DOMUS_API_BASE_URL`.  
**Alternatives:** Default Kestrel ports — breaks the front without env edits.

### D8: Path `/me` without version prefix
**Choice:** Expose `/me` exactly as the frontend client calls today.  
**Why:** Zero front path change for reads; `POST /me` is additive.  
**Alternatives:** `/v1/me` — requires coordinated front update; defer until a versioning strategy exists.

## Risks / Trade-offs

- **[Risk] Logto API resource / audience misconfigured** → Tokens fail validation; document `Authentication__Audience` and align with `VITE_LOGTO_API_RESOURCE` before E2E.  
- **[Risk] Frontend still describes operator-only provisioning** → API ships `POST /me`; front needs a follow-up to offer self-serve in the `403` state.  
- **[Risk] Self-serve means any valid Logto identity can become a Domus User** → Accept for milestone 1; House membership remains the future access gate for household data.  
- **[Trade-off] Single project** → Simpler now; may split later when capabilities multiply.  
- **[Trade-off] Remote Railway Postgres from local API** → Slight latency; avoids local DB ops for this milestone.

## Migration Plan

1. Provision/confirm Railway Postgres; obtain connection string for local `.env`.
2. Confirm Logto API resource indicator; set API audience + front `VITE_LOGTO_API_RESOURCE`.
3. Implement API, apply migration to Railway Postgres.
4. Verify `GET /me` outcomes `401` / `403` / `200` and `POST /me` `201` / `409`.
5. Frontend follow-up (separate repo): wire self-serve action on unprovisioned state.
6. Rollback: stop API / revert deploy; User rows may remain in Postgres (harmless); IdP users untouched.

## Open Questions

- Exact env var naming convention in deploy (Railway variable mapping) can be finalized at deploy time without changing specs.
