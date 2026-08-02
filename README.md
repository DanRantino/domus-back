# Domus API

Backend .NET da Domus. Primeiro marco: capability **Users** + validação JWT Logto + `GET/POST /me`.

## Requisitos

- .NET 8 SDK
- PostgreSQL (Railway)
- API resource configurado no Logto (audience alinhado com o front)

## Configuração

Copie [`.env.example`](.env.example) para `.env` e preencha:

| Variável | Descrição |
|----------|-----------|
| `Authentication__Authority` | Issuer OIDC Logto (`…/oidc`) |
| `Authentication__Audience` | API resource / `aud` (mesmo valor que `VITE_LOGTO_API_RESOURCE` no front) |
| `DATABASE_URL` ou `ConnectionStrings__Database` | Postgres Railway |

Porta local: `http://localhost:3001` (CORS permite `http://localhost:3000`).

## Executar

```bash
export $(grep -v '^#' .env | xargs)   # ou exporte as vars manualmente
dotnet ef database update --project src/Domus.Api
dotnet run --project src/Domus.Api
```

Se `dotnet-ef` não estiver no PATH:

```bash
dotnet tool install --global dotnet-ef --version 8.0.19
# ou use o tool-path local: ./.dotnet-cli/dotnet-ef
```

## Endpoints

| Método | Path | Auth | Resultado |
|--------|------|------|-----------|
| `GET` | `/me` | Bearer JWT | `401` / `403` (não provisionado) / `200` `{ id, identity_id }` |
| `POST` | `/me` | Bearer JWT | `201` cria User do `sub` / `409` já existe / `401` |

`GET /me` nunca provisiona. `POST /me` ignora body — `identity_id` vem só do token.

## Testes

```bash
dotnet test Domus.sln
```

## Follow-up no frontend

O front já chama `GET /me`. Para fechar o fluxo self-serve, no estado `not_provisioned` (HTTP 403) a UI deve oferecer uma ação explícita que chama `POST /me` com o mesmo Bearer token, depois revalidar com `GET /me`. Isso fica no repositório `front` (fora deste repo).
