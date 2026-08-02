## 1. Project foundation

- [x] 1.1 Create solution and `Domus.Api` ASP.NET Core project under `src/`
- [x] 1.2 Configure local listen URL `http://localhost:3001` and commit `.env.example` for required configuration (database, OIDC authority, audience)
- [x] 1.3 Add CORS allowing the SPA origin `http://localhost:3000`
- [x] 1.4 Fail fast at startup when required configuration is missing or invalid

## 2. Authentication

- [x] 2.1 Add JWT Bearer authentication against the Logto OIDC issuer (signature, issuer, audience, expiration)
- [x] 2.2 Ensure protected endpoints return HTTP 401 for missing or invalid tokens
- [x] 2.3 Confirm no per-request Logto Management/API calls are used for token validation

## 3. Persistence

- [x] 3.1 Add EF Core + Npgsql and a `users` entity with `id` (UUID) and `identity_id` (unique)
- [x] 3.2 Create and apply the initial migration against Railway PostgreSQL
- [x] 3.3 Wire `DbContext` into DI using the environment connection string (SSL as required by Railway)

## 4. Users endpoints

- [x] 4.1 Implement `GET /me`: resolve User by token `sub`; return `401` / `403` / `200` with snake_case `{ id, identity_id }`; never provision on read
- [x] 4.2 Implement `POST /me`: create User from authenticated `sub` only; return `201` with body on create; return `409` if already provisioned; return `401` if unauthenticated
- [x] 4.3 Reject any attempt to supply an arbitrary `identity_id` from the request body for provisioning

## 5. Tests

- [x] 5.1 Add integration/API tests for `GET /me` outcomes: unauthenticated → 401, authenticated unprovisioned → 403, provisioned → 200
- [x] 5.2 Add tests for `POST /me`: first call → 201 and persisted User; second call → 409; unauthenticated → 401
- [x] 5.3 Add a test that provisioning uses the token `sub` and ignores forged body identity values

## 6. Verification

- [x] 6.1 Smoke-check locally: API on `:3001`, JWT validation with configured audience, migration applied
- [x] 6.2 Document any remaining frontend follow-up (`POST /me` from unprovisioned UI) without implementing it in this repo
