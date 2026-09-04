# Domus API

Backend .NET da Domus. Monólito modular: **Domain / Application / Infrastructure / Api**. Capability inicial: **Users**. Autenticação: cookie HttpOnly via SDK Logto MVC (`AddLogtoAuthentication`) para o SPA, mais JWT Bearer para clientes não-browser.

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
| `Authentication__Authority`                     | Issuer OIDC Logto (`…/oidc`) — validação JWT Bearer                       |
| `Authentication__Audience`                      | API resource / `aud` (JWT + `options.Resource` do SDK)                    |
| `Logto__Endpoint`                               | URL do tenant **com barra final** (`https://auth.domus.dev/`). Não use `…/oidc` |
| `Logto__AppId`                                  | App ID do Traditional Web App no Console Logto                            |
| `Logto__AppSecret`                              | App secret do Traditional Web App (só na API, nunca no front)             |
| `DATABASE_URL` ou `ConnectionStrings__Database` | Postgres Railway                                                          |
| `Cors__Origins__0`                              | Origem **pública** do SPA (local: `https://web.domus.dev`; Railway: `https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}`) |
| `Resend__ApiKey`                                | API key do Resend para e-mail de convite. Vazio em Development só registra o e-mail no log |
| `Resend__From`                                  | Remetente verificado no Resend (`Nome <email@dominio>`)                   |
| `Invitations__FrontendOrigin`                   | Origem pública do SPA usada no link do convite (`https://web.domus.dev`) |

No Dev Container a API escuta em `PORT=5000` (`https://api.domus.dev` e os caminhos same-origin em `https://web.domus.dev`). O SPA é `https://web.domus.dev`.

### Console Logto (Traditional Web App)

Crie um aplicativo **Traditional Web** (não SPA) por ambiente, como no [tutorial MVC](https://docs.logto.io/pt-BR/quick-starts/dotnet-core/mvc). Redirect URIs na origem do **front**:

| Ambiente | Redirect URI | Post sign-out redirect URI |
| --- | --- | --- |
| Local | `https://web.domus.dev/Callback` | `https://web.domus.dev/SignedOutCallback` |
| Railway | `https://<domínio-público-do-front>/Callback` | `https://<domínio-público-do-front>/SignedOutCallback` |

`api.domus.dev` continua para Swagger e Bearer direto. O Caddy do front encaminha `/Callback`, `/SignedOutCallback`, `/auth/*` e `/api/*` para esta API.

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
| `GET`   | `/auth/login`   | não        | Challenge OIDC (redirect para o Logto); `returnUrl` relativo (`/` ou `/dashboard`)                  |
| `GET`   | `/auth/logout`  | não        | Sign-out OIDC + cookie; `returnUrl` relativo                                                        |
| `GET`   | `/auth/session` | cookie opcional | `{ authenticated, picture, name }` (sem envelope); anônimo → `authenticated: false`            |
| `GET`   | `/users/me`     | cookie ou Bearer | `401` / `403` + `not_provisioned` / `200` + envelope com perfil, settings e houses            |
| `POST`  | `/users/me`     | cookie ou Bearer | `201` + envelope (defaults) / `409` + `already_exists` / `401`                                 |
| `PATCH` | `/users/me`     | cookie ou Bearer | atualiza `full_name` (opcional; null/"" limpa)                                                 |
| `PATCH` | `/users/me/settings` | cookie ou Bearer | atualiza `theme` e/ou `notifications` (merge parcial); `400` + `validation_error` se theme inválido |
| `GET`   | `/houses`       | cookie ou Bearer | `401` / `403` + `not_provisioned` / `200` + envelope com as casas do caller (lista pode ser vazia) |
| `GET`   | `/houses/{id}`  | cookie ou Bearer | `401` / `403` + `not_provisioned` / `200` + envelope da casa / `404` + `not_found` se não for membro |
| `POST`  | `/houses`       | cookie ou Bearer | `201` + envelope da casa (`role=admin`) / `400` + `validation_error` / `401` / `403` + `not_provisioned` |
| `POST`  | `/houses/{id}/invitations` | cookie ou Bearer | admin: `201` convite pendente / `403` / `409` duplicado / `400` role ou e-mail inválido |
| `GET`   | `/houses/{id}/invitations` | cookie ou Bearer | admin: lista pendentes da casa |
| `DELETE`| `/houses/{id}/invitations/{invitationId}` | cookie ou Bearer | admin: revoga pendente |
| `POST`  | `/houses/{id}/invitations/{invitationId}/resend` | cookie ou Bearer | admin: rotaciona token e reenvia e-mail |
| `GET`   | `/invitations/preview` | não | `200` + `house_name` / `404` token inválido; não expõe o e-mail do convidado |
| `POST`  | `/invitations/accept` | cookie ou Bearer | `200` cria membership se o e-mail do IdP coincidir / `403` / `404` / `409` já membro |

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
  Logto__Endpoint=https://logto-auth-preprod.up.railway.app/ \
  Logto__AppId=<traditional-web-app-id> \
  Cors__Origins__0='https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}' \
  --service Domus.Api

printf '%s' "$LOGTO_APP_SECRET" | railway variable set Logto__AppSecret --stdin --service Domus.Api

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
| `Authentication__Audience`  | API resource / `aud`                                                                              |
| `Logto__Endpoint`           | Tenant Logto **com barra final** (ex. `https://logto-auth-preprod.up.railway.app/`)               |
| `Logto__AppId`              | App ID do Traditional Web App                                                                     |
| `Logto__AppSecret`          | App secret do Traditional Web App (stdin; não no bundle do front)                                 |
| `DATABASE_URL`              | Referência privada `${{Postgres.DATABASE_URL}}` (não use a URL pública do TCP proxy)              |
| `Cors__Origins__0`          | Origem **pública** do SPA: `https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}` (não use DNS interno) |

Não defina `ASPNETCORE_URLS=http://localhost:3001` no Railway. A app lê `PORT` e escuta em `0.0.0.0:$PORT`.

### Verificação

```bash
curl -s https://<api-domain>/health/live
curl -s https://<api-domain>/health/ready
curl -s -o /dev/null -w '%{http_code}\n' https://<api-domain>/users/me   # esperado: 401
curl -s https://<api-domain>/auth/session   # esperado: {"authenticated":false,...}
```

No front, `VITE_DOMUS_API_BASE_URL=/api` (same-origin). O Caddy do SPA encaminha `/api/*` para a API na rede privada (`DOMUS_API_UPSTREAM`). O browser não alcança `*.railway.internal`.

## Follow-up no frontend

O front orquestra self-serve (`GET /me` → 403 → `POST /me` → `GET /me`) e precisa ler o envelope. Detalhes no repositório `front`.
