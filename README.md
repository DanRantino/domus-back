# Domus API

Backend .NET da Domus. Monólito modular: **Domain / Application / Infrastructure / Api**. Capability inicial: **Users** + JWT Logto + `GET/POST /me`.

## Requisitos

- .NET SDK 10.0.302
- PostgreSQL 16
- Dev Container do Domus
- API resource configurado no Logto

## Migrations

As migrations ficam em `src/Domus.Infrastructure/Migrations` e são a fonte de verdade para evolução do schema.

O `dotnet-ef` é controlado pelo tool manifest do próprio repositório. Não instale uma versão global para trabalhar no Domus.

Restaure as ferramentas locais:

```bash
dotnet tool restore

# Para aplicar as migrations:

ConnectionStrings__Database='<connection-string>' \
dotnet ef database update \
  --project src/Domus.Infrastructure \
  --startup-project src/Domus.Api

```

## Configuração

Copie [`.env.example`](.env.example) para `.env` e preencha:

| Variável                                        | Descrição                                                                 |
| ----------------------------------------------- | ------------------------------------------------------------------------- |
| `Authentication__Authority`                     | Issuer OIDC Logto (`…/oidc`)                                              |
| `Authentication__Audience`                      | API resource / `aud` (mesmo valor que `VITE_LOGTO_API_RESOURCE` no front) |
| `DATABASE_URL` ou `ConnectionStrings__Database` | Postgres Railway                                                          |
| `Cors__Origins__0`                              | Origem do SPA (default local: `http://localhost:3000`)                    |

Porta local: `http://localhost:3001` (CORS permite `http://localhost:3000`).

## Executar

```bash
export $(grep -v '^#' .env | xargs)   # ou exporte as vars manualmente
dotnet run --project src/Domus.Api
```

Migrations rodam automaticamente no startup quando o provider é PostgreSQL.

Se precisar aplicar migrations manualmente:

ConnectionStrings\_\_Database='<connection-string>' \
dotnet ef database update \
 --project src/Domus.Infrastructure \
 --startup-project src/Domus.Api

````

## Envelope de resposta (produto)

Endpoints de produto usam envelope JSON (snake_case):

```json
{ "success": true, "data": { }, "error": null }
{ "success": false, "data": null, "error": { "code": "…", "message": "…" } }
````

**BREAKING para o Domus Web:** o body de sucesso de `/me` usa envelope (`data`) e a representação do user inclui `full_name`, `settings` (`theme`, `notifications` por categoria) e `houses`. Erros de produto usam `error.code` (`not_provisioned`, `already_exists`, `validation_error`). Status HTTP continuam significativos.

Troca de senha **não** é endpoint Domus — o client redireciona para a experiência de conta do IdP (Logto).

Health checks **não** usam o envelope de produto.

## Endpoints

| Método  | Path            | Auth       | Resultado                                                                                           |
| ------- | --------------- | ---------- | --------------------------------------------------------------------------------------------------- |
| `GET`   | `/health/live`  | não        | `200` se o processo está vivo                                                                       |
| `GET`   | `/health/ready` | não        | `200` se o Postgres está alcançável; caso contrário não-sucesso                                     |
| `GET`   | `/me`           | Bearer JWT | `401` / `403` + `not_provisioned` / `200` + envelope com perfil, settings e houses                  |
| `POST`  | `/me`           | Bearer JWT | `201` + envelope (defaults) / `409` + `already_exists` / `401`                                      |
| `PATCH` | `/me`           | Bearer JWT | atualiza `full_name` (opcional; null/"" limpa)                                                      |
| `PATCH` | `/me/settings`  | Bearer JWT | atualiza `theme` e/ou `notifications` (merge parcial); `400` + `validation_error` se theme inválido |

`GET /me` nunca provisiona. `POST /me` ignora body — `identity_id` vem só do token. Settings default no provisionamento: `theme=system`, notificações `daily_tasks` / `expenses` / `family_chat` = `true`.

## Testes

```bash
dotnet test Domus.sln
```

## Deploy no Railway

Railpack não suporta .NET: o build usa o [`Dockerfile`](Dockerfile) na raiz.

Logs da aplicação vão para **stdout** (JSON console) e aparecem no log do Railway. Não há stack de logging externo neste marco.

### Serviço

Configuração de build/deploy: [`railway.toml`](railway.toml) (Dockerfile + healthcheck `/health/live`).

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

| Variável                    | Valor                                                                                             |
| --------------------------- | ------------------------------------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`    | `Production`                                                                                      |
| `Authentication__Authority` | Issuer Logto (ex. `https://logto-auth-preprod.up.railway.app/oidc`)                               |
| `Authentication__Audience`  | API resource / `aud` (igual ao front)                                                             |
| `DATABASE_URL`              | Referência privada `${{Postgres.DATABASE_URL}}` (não use a URL pública do TCP proxy)              |
| `Cors__Origins__0`          | Origem do front em produção (ex. `https://….up.railway.app` ou `http://localhost:3000` em testes) |

Não defina `ASPNETCORE_URLS=http://localhost:3001` no Railway. A app lê `PORT` e escuta em `0.0.0.0:$PORT`.

### Verificação

```bash
curl -s https://<api-domain>/health/live
curl -s https://<api-domain>/health/ready
curl -s -o /dev/null -w '%{http_code}\n' https://<api-domain>/me   # esperado: 401
```

No front, aponte `VITE_DOMUS_API_BASE_URL` para o domínio da API e ajuste o parse de `/me` para o envelope (`data` / `error.code`).

## Follow-up no frontend

O front orquestra self-serve (`GET /me` → 403 → `POST /me` → `GET /me`) e precisa ler o envelope. Detalhes no repositório `front`.
