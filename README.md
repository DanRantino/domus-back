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
| `Cors__Origins__0`                              | Origem **pública** do SPA (local: `https://web.domus.dev`; Railway: `https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}`) |

Porta local da API: `http://localhost:3001` (atrás do Caddy: `https://api.domus.dev`). O SPA local é `https://web.domus.dev` — configure `Cors__Origins__0` com essa origem.

O Railway CLI vem no Dev Container. Secrets de serviço (M2M, audience, `DATABASE_URL` de preprod/prod) ficam no Railway — não no `.env` versionado. Depois de `railway login` e `railway link` neste repositório:

```bash
railway variable list --service Domus.Api
railway variable list --service Domus.Api --kv

# Secret sem aparecer no histórico do shell
printf '%s' "$SECRET" | railway variable set DevelopmentSeed__ClientSecret --stdin --service Domus.Api
```

## Executar

```bash
dotnet run --project src/Domus.Api
```

O `.env` na raiz do repositório é carregado automaticamente se existir. Variáveis já definidas no ambiente não são sobrescritas.

Migrations rodam automaticamente no startup quando o provider é PostgreSQL.

Se precisar aplicar migrations manualmente:

ConnectionStrings\_\_Database='<connection-string>' \
dotnet ef database update \
 --project src/Domus.Infrastructure \
 --startup-project src/Domus.Api

## Seed de desenvolvimento

Garante usuários no Logto, os mesmos usuários no Postgres, as casas e os vínculos (`house_memberships`). Não sobe o servidor HTTP.

```bash
dotnet run --project src/Domus.Api -- --seed
```

O `--` entrega `--seed` para a aplicação. Além do banco (`DATABASE_URL` ou `ConnectionStrings__Database`), o comando precisa das variáveis M2M em [`.env.example`](.env.example):

- `DevelopmentSeed__LogtoEndpoint`
- `DevelopmentSeed__ManagementApiResource`
- `DevelopmentSeed__ClientId`
- `DevelopmentSeed__ClientSecret`

O que o seed garante:

| Casa            | Email               | Papel    |
| --------------- | ------------------- | -------- |
| Casa da Família | `dev1@domus.local`  | `admin`  |
| Casa da Família | `dev2@domus.local`  | `member` |
| Casa da Família | `dev3@domus.local`  | `member` |
| Casa da Família | `dev4@domus.local`  | `member` |
| Casa do Admin   | `dev1@domus.local`  | `admin`  |

Pode rodar de novo: não duplica usuários, casas nem memberships, e não altera linhas que já existem. No Logto, só cria quem falta e só atualiza se `name` ou `username` estiverem diferentes do catálogo.

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

railway variable set \
  ASPNETCORE_ENVIRONMENT=Production \
  Authentication__Authority=https://logto-auth-preprod.up.railway.app/oidc \
  Authentication__Audience=<seu-api-resource> \
  Cors__Origins__0='https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}' \
  --service Domus.Api

# DATABASE_URL privada do Postgres (ajuste o nome do serviço se for outro)
railway variable set DATABASE_URL='${{Postgres.DATABASE_URL}}' --service Domus.Api

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
| `Cors__Origins__0`          | Origem **pública** do SPA: `https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}` (não use DNS interno) |

Não defina `ASPNETCORE_URLS=http://localhost:3001` no Railway. A app lê `PORT` e escuta em `0.0.0.0:$PORT`.

### Verificação

```bash
curl -s https://<api-domain>/health/live
curl -s https://<api-domain>/health/ready
curl -s -o /dev/null -w '%{http_code}\n' https://<api-domain>/me   # esperado: 401
```

No front, `VITE_DOMUS_API_BASE_URL` deve ser a URL **pública** da API (`https://${{domus-back.RAILWAY_PUBLIC_DOMAIN}}`). O browser não alcança `*.railway.internal`.

## Follow-up no frontend

O front orquestra self-serve (`GET /me` → 403 → `POST /me` → `GET /me`) e precisa ler o envelope. Detalhes no repositório `front`.
