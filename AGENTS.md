# AGENTS.md

## Cursor Cloud specific instructions

Domus API is a single .NET 10 service (`src/Domus.Api`, a modular monolith: Domain / Application / Infrastructure / Api) backed by PostgreSQL 16 and JWT auth via Logto. Standard commands (build/test/run/migrations/seed) are documented in `README.md`; only the non-obvious cloud caveats are listed here.

### Toolchain (baked into the VM snapshot)
- .NET SDK `10.0.302` lives in `/usr/local/dotnet` and is symlinked to `/usr/local/bin/dotnet` (already on `PATH`). Do not use nvm-style shims; `global.json` pins the SDK version.
- The `dotnet-ef` CLI is a **local** tool (`dotnet-tools.json`), invoked as `dotnet ef ...` after `dotnet tool restore`. Never install a global `dotnet-ef`.
- PostgreSQL 16 is installed but **not auto-started**. Start it each boot with: `sudo pg_ctlcluster 16 main start`.

### Local dev database + `.env` (required to run the API)
- The API throws at startup unless `Authentication__Authority`, `Authentication__Audience`, and a DB connection string (`ConnectionStrings__Database` or `DATABASE_URL`) are all set. These are supplied by a gitignored `.env` at the repo root, which `DotEnvLoader` loads automatically.
- Local Postgres dev credentials: role `domus` / password `domus` / database `domus`. If the role/db is missing after a fresh boot, recreate it:
  ```bash
  sudo -u postgres psql -c "CREATE ROLE domus LOGIN PASSWORD 'domus';"
  sudo -u postgres createdb -O domus domus
  ```
- If `.env` is missing, recreate it at the repo root with `ASPNETCORE_URLS=http://localhost:3001`, a non-empty `Authentication__Authority`/`Authentication__Audience`, and `ConnectionStrings__Database=Host=localhost;Port=5432;Database=domus;Username=domus;Password=domus`.
- EF migrations auto-apply on startup when the provider is PostgreSQL — no manual `dotnet ef database update` needed for local runs. The API listens on `http://localhost:3001`.

### Testing caveats
- `dotnet test Domus.sln` needs **neither PostgreSQL nor Logto**: the test host uses SQLite in-memory and a fake auth handler (`tests/Domus.Api.Tests/Support`). It runs fully offline.
- `dotnet format Domus.sln --verify-no-changes` is the closest lint gate (there is no `.editorconfig`). Note: the repo currently has one pre-existing whitespace deviation in `src/Domus.Domain/Users/User.cs`, so this command exits non-zero on an unmodified tree.

### Known limitation (live-server user flow)
- The `/users/me` endpoints require a real Logto-issued JWT, so on the live server they return `401` without one. Exercising the actual provisioning flow end-to-end (or running `dotnet run --project src/Domus.Api -- --seed`) needs the Logto M2M secrets from `.env.example` (`DevelopmentSeed__*`). The full `/me` create/read/update behavior is otherwise covered by the automated tests.
