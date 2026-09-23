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
