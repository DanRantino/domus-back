# Domus API — Agent Entry Point

**Repository:** `domus-back` (local checkout often named `domus-api`).  
**.NET modular monolith:** Domain / Application / Infrastructure / Api.  
**Responsibility:** Domus HTTP + GraphQL, Logto auth (cookie BFF + JWT Bearer), PostgreSQL (EF Core), house/membership/invitation/task rules.

Do **not** read the full product docs before starting. Use this file + the PR diff (review) or the code in the area you change (implementation).

## Shared context (only when the task crosses repos)

Owned by **`domus-dev`** (typical sibling checkout):

- [`../domus-dev/docs/agents/CONTEXT.md`](../domus-dev/docs/agents/CONTEXT.md) — system context
- [`../domus-dev/docs/agents/ROLES.md`](../domus-dev/docs/agents/ROLES.md) — Cursor vs Codex roles
- [`../domus-dev/docs/agents/INDEX.md`](../domus-dev/docs/agents/INDEX.md) — topic index

Open those **only** when the task crosses repo boundaries (front, infra, product). Product direction, when needed, is `domus-web/docs/product/domus-overview.md`. Implemented behavior is the code.

Frontend entry (contracts / UI callers): [`../domus-web/AGENTS.md`](../domus-web/AGENTS.md).

## Branch and PR review

- Feature base: `nonprod` (never branch from `main`). See [`.cursor/rules/branching.mdc`](.cursor/rules/branching.mdc).
- **PR review (Codex):** read the diff and adjacent code; do not require a local WSL/Dev Container connection or implement fixes for a normal review. Role guidance: [`../domus-dev/docs/agents/ROLES.md`](../domus-dev/docs/agents/ROLES.md).

## Layout (real paths)

| Layer | Path | Contents |
| --- | --- | --- |
| Api | `src/Domus.Api/` | `Program.cs`, controllers, contracts, GraphQL, HTTP middleware |
| Application | `src/Domus.Application/` | use cases (`*Service`), `AppResult` / `ErrorCodes`, persistence interfaces |
| Domain | `src/Domus.Domain/` | entities and domain constants (e.g. `Houses/HouseRoles.cs`) |
| Infrastructure | `src/Domus.Infrastructure/` | EF Core, migrations, seed, Logto M2M, mail |
| Tests | `tests/Domus.Api.Tests/` | endpoint, service, and persistence tests |

HTTP entry: `src/Domus.Api/Controllers/`. GraphQL: `src/Domus.Api/GraphQL/` at `/graphql` (`Program.cs`). JSON envelope: `src/Domus.Api/Http/`.

## Local conventions (already in repo)

- [`.cursor/rules/architecture.mdc`](.cursor/rules/architecture.mdc) — layer boundaries (always)
- [`.cursor/rules/backend-architecture.mdc`](.cursor/rules/backend-architecture.mdc) — C#, `AppResult`, tests
- [`.cursor/rules/api.mdc`](.cursor/rules/api.mdc) — ASP.NET controllers
- [`.cursor/rules/branching.mdc`](.cursor/rules/branching.mdc) — git / PR → `nonprod`
- [`README.md`](README.md) — setup, env **names**, endpoints, seed (validate against code; see “docs vs code” below)

REST contracts: `src/Domus.Api/Contracts/` + controllers. OpenAPI/Swagger in Development. GraphQL `me`: `GraphQL/Query.cs` / `Me.cs`.

## Auth and authorization

**Authentication (IdP):** Logto — HttpOnly cookie (SPA/BFF) + JWT Bearer; policy scheme `CookieOrBearer` in `Program.cs` / `Http/DomusAuthSchemes.cs`. Login/logout/session: `Controllers/AuthController.cs`.

**Provisioning:** `Http/CurrentUserMiddleware.cs` loads the Domus `User` into context when IdP `sub` matches `identity_id`. Controllers use `Http/CurrentUserContext.cs` (`TryRequire*`): unauthenticated → **401**; IdP-authenticated but no Domus user → **403** `not_provisioned`. Explicit provision: `Controllers/UserController.cs` (`UsersController`) + `MeService.ProvisionAsync`.

**House authorization (membership):** no separate permissions engine. Roles in `src/Domus.Domain/Houses/HouseRoles.cs` (`admin`, `member`, `guest`). Membership entity `HouseMembership`; read `IHouseMembershipReader` / `HouseMembershipReader`; writes via `IHouseWriter` (same reader implements create/add).

Rules **implemented** (confirm in code/tests before extending):

| Rule | Where | Tests |
| --- | --- | --- |
| List/get house only if member; create → caller becomes `admin` | `Application/Houses/HouseService.cs`, `Controllers/HousesController.cs` | `tests/.../HousesEndpointTests.cs` |
| Invitations (create/list/revoke/resend) require house `admin` (`RequireAdminAsync`) | `Application/Houses/InvitationService.cs`, `Controllers/HouseInvitationsController.cs` | `InvitationsEndpointTests.cs`, `InvitationServiceTests.cs` (`Create_NonAdmin_IsForbidden`) |
| Accept invite: IdP email must match; creates membership | `InvitationService.AcceptAsync`, `Controllers/InvitationsController.cs` | same test files |
| Complete task: any house **member** (role not distinguished); non-member → `not_found` | `Application/Tasks/HouseTaskService.cs`, `Controllers/HouseTasksController.cs` | `HouseTaskServiceTests.cs`, `HouseTasksEndpointTests.cs` |

`HouseRoles.Guest` exists in domain, but **invite with role `guest` is rejected** (`validation_error`) — see `Create_RejectsGuestRole` / `CreateInvitation_GuestRole_Returns400`. Do not invent permission endpoints; confirm in current code.

## Persistence, migrations, seed

- Migrations: `src/Domus.Infrastructure/Migrations/` (schema source of truth).
- DbContext: `src/Domus.Infrastructure/Persistence/DomusDbContext.cs`.
- Startup runs `Migrate()` when the provider is PostgreSQL (`Program.cs`).
- Dev seed (Logto + Postgres + houses/memberships/tasks): `src/Domus.Infrastructure/DevelopmentSeed/` — CLI `--seed` (does not start HTTP).

## Commands (this repo)

SDK: `global.json` → .NET **10.0.302**. CI: [`.github/workflows/ci.yml`](.github/workflows/ci.yml).

```bash
dotnet restore Domus.sln
dotnet build Domus.sln --configuration Release --no-restore
dotnet test Domus.sln --configuration Release --no-build --blame-hang-timeout 2min --blame-hang-dump-type none
```

Day-to-day local:

```bash
dotnet test Domus.sln
dotnet run --project src/Domus.Api
dotnet tool restore
ConnectionStrings__Database='<connection-string>' \
  dotnet ef database update \
  --project src/Domus.Infrastructure \
  --startup-project src/Domus.Api
dotnet run --project src/Domus.Api -- --seed
```

No dedicated `dotnet format` / analyzer job in CI: static check is the **build** (nullable + SDK warnings). Do not use Infisical or invent Makefile / dynamic Railway substitution flows here. Env **names** only: [`.env.example`](.env.example) / README.

## Constraints in this repo

- Backend only: do not change `domus-web` / `domus-dev` from tasks scoped here.
- Respect Domain → Application → Infrastructure → Api; EF/`DbContext` stay in Infrastructure.
- Controllers have no business rules; Application returns `AppResult`, Api maps HTTP.
- Do not invent endpoints, roles, or “planned” flows without code/tests.
- No secrets in git; do not copy production values into a versioned `.env`.
- Do not approve/merge PRs; wait for a human ([`branching.mdc`](.cursor/rules/branching.mdc)).

## Docs vs code (`nonprod` — always verify)

| Item | Situation |
| --- | --- |
| `GET/POST /users/me`, houses, invitations, auth, health | **Implemented** (controllers + tests) |
| `POST /houses/{houseId}/tasks/{taskId}/complete`, GraphQL `me` at `/graphql` | **Implemented** in code; **missing** from the README endpoint table |
| `PATCH /users/me`, `PATCH /users/me/settings` | Documented in README; **no** matching actions on `UsersController` / `MeService` on `nonprod` — treat as **stale/planned**, do not implement from the README alone |
| Role `guest` on invitations | Domain constant exists; **rejected** on create invitation |

When unsure, code and tests under `tests/Domus.Api.Tests/` win over the README.
