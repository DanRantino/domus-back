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
| `Cors__Origins__0` | Origem do SPA (default local: `http://localhost:3000`) |

Porta local: `http://localhost:3001` (CORS permite `http://localhost:3000`).

## Executar

```bash
export $(grep -v '^#' .env | xargs)   # ou exporte as vars manualmente
dotnet run --project src/Domus.Api
```

Migrations rodam automaticamente no startup quando o provider é PostgreSQL.

Se precisar aplicar migrations manualmente:

```bash
dotnet tool install --global dotnet-ef --version 8.0.19
dotnet ef database update --project src/Domus.Api
```

## Endpoints

| Método | Path | Auth | Resultado |
|--------|------|------|-----------|
| `GET` | `/health` | não | `200` `{ "status": "ok" }` |
| `GET` | `/me` | Bearer JWT | `401` / `403` (não provisionado) / `200` `{ id, identity_id }` |
| `POST` | `/me` | Bearer JWT | `201` cria User do `sub` / `409` já existe / `401` |

`GET /me` nunca provisiona. `POST /me` ignora body — `identity_id` vem só do token.

## Testes

```bash
dotnet test Domus.sln
```

## Deploy no Railway

Railpack não suporta .NET: o build usa o [`Dockerfile`](Dockerfile) na raiz.

### Serviço

Configuração de build/deploy: [`railway.toml`](railway.toml) (Dockerfile + healthcheck `/health`).

No diretório deste repo (projeto já linkado ao Postgres):

```bash
# Criar serviço vazio e fazer deploy do diretório atual
railway add --service Domus.Api
railway service Domus.Api   # ou: railway link --service Domus.Api

railway variables set \
  ASPNETCORE_ENVIRONMENT=Production \
  Authentication__Authority=https://logto-auth-preprod.up.railway.app/oidc \
  Authentication__Audience=<seu-api-resource> \
  Cors__Origins__0=http://localhost:3000

# DATABASE_URL privada do Postgres (ajuste o nome do serviço se for outro)
railway variables set DATABASE_URL='${{Postgres.DATABASE_URL}}'

railway domain
railway up
```

Alternativa no dashboard: New Service → GitHub `DanRantino/domus-back` → o `Dockerfile` / `railway.toml` são detectados automaticamente.

### Variáveis do serviço API

| Variável | Valor |
|----------|--------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Authentication__Authority` | Issuer Logto (ex. `https://logto-auth-preprod.up.railway.app/oidc`) |
| `Authentication__Audience` | API resource / `aud` (igual ao front) |
| `DATABASE_URL` | Referência privada `${{Postgres.DATABASE_URL}}` (não use a URL pública do TCP proxy) |
| `Cors__Origins__0` | Origem do front em produção (ex. `https://….up.railway.app` ou `http://localhost:3000` em testes) |

Não defina `ASPNETCORE_URLS=http://localhost:3001` no Railway. A app lê `PORT` e escuta em `0.0.0.0:$PORT`.

### Verificação

```bash
curl -s https://<api-domain>/health
curl -s -o /dev/null -w '%{http_code}\n' https://<api-domain>/me   # esperado: 401
```

No front, aponte `VITE_DOMUS_API_BASE_URL` para o domínio da API.

## Follow-up no frontend

O front orquestra self-serve (`GET /me` → 403 → `POST /me` → `GET /me`). Detalhes no repositório `front`.
