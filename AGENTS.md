# Domus API — agent entry

**Repository:** `domus-back` (local checkout often named `domus-api`).
**.NET modular monolith:** Domain / Application / Infrastructure / Api.
**Responsibility:** HTTP + GraphQL, Logto cookie BFF + JWT Bearer, PostgreSQL (EF Core).

Implemented behavior is the code on the branch you change. Product direction, when a task needs it: `../domus-web/docs/product/domus-overview.md`.

## Commands

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

SDK: `global.json` (.NET 10). CI: [`.github/workflows/ci.yml`](.github/workflows/ci.yml). Env names: [`.env.example`](.env.example). No Makefile or Infisical.

## When to read

- Layers or a new persistence boundary: [`.cursor/rules/architecture.mdc`](.cursor/rules/architecture.mdc).
- C# and `AppResult`: [`.cursor/rules/backend-architecture.mdc`](.cursor/rules/backend-architecture.mdc) (glob `*.cs`).
- Controllers: [`.cursor/rules/api.mdc`](.cursor/rules/api.mdc) (glob `*Controller.cs`).
- Cross-repo work: [`../domus-dev/docs/agents/INDEX.md`](../domus-dev/docs/agents/INDEX.md), then one topic.
- Session flow: [`../domus-dev/docs/agents/auth.md`](../domus-dev/docs/agents/auth.md). Implemented versus planned: [`../domus-dev/docs/agents/CONTEXT.md`](../domus-dev/docs/agents/CONTEXT.md).

## Constraints

- Backend only. Do not change `domus-web` or `domus-dev` unless the task asks.
- Domain, then Application, then Infrastructure, then Api. EF and `DbContext` stay in Infrastructure.
- Controllers have no business rules. Application returns `AppResult`; Api maps HTTP.
- Do not invent endpoints, roles, or planned flows. Confirm in `tests/Domus.Api.Tests/`.
- No secrets in git. Branch from `nonprod`: [`.cursor/rules/branching.mdc`](.cursor/rules/branching.mdc).
