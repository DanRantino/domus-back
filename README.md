# Domus API

Backend .NET da Domus. Monólito modular: **Domain / Application / Infrastructure / Api**. Capability inicial: **Users**. Autenticação: cookie HttpOnly via SDK Logto MVC (`AddLogtoAuthentication`) para o SPA, mais JWT Bearer para clientes não-browser.

## Requisitos

- .NET SDK 10.0.302
- PostgreSQL 16
- Dev Container do Domus
- API resource configurado no Logto

Dependências: [`docs/dependency-security.md`](docs/dependency-security.md).

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

## Perfis de ambiente

A aplicação é uma só. O perfil entra por configuração (`ASPNETCORE_ENVIRONMENT` e variáveis de ambiente), sem ramo de código que escolha local ou Railway.

| | Local | Railway Development (`preprod`) | Production (`prod`) |
| --- | --- | --- | --- |
| Onde a API roda | máquina / Dev Container | serviço `domus-back` | serviço `domus-back` |
| Onde ficam os valores | `.env`, copiado de [`.env.example`](.env.example) | variáveis do environment `preprod` | variáveis do environment `prod` |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Development` | `Production` |
| Postgres | Postgres 16 do [`domus-dev`](https://github.com/DanRantino/domus-dev) em `127.0.0.1:5432` | `DATABASE_URL` privado daquele environment | `DATABASE_URL` privado daquele environment |
| Identidades | `https://api.domus.dev`, SPA `https://web.domus.dev`, Logto Cloud `https://9vhnmt.logto.app/` | domínios públicos de `domus-back` / `domus-front` e o tenant Logto do `preprod` | domínios e tenant de prod |
| Resend | key vazia: o convite só vai para o log | mesma regra de Development | `Resend__ApiKey` obrigatória |

Seleção:

- **Local:** `cp .env.example .env`, Postgres do `domus-dev` (Dev Container ou `docker compose up -d` naquele repositório), `dotnet run --project src/Domus.Api`.
- **Railway Development:** não há arquivo de secrets. No serviço `domus-back`, environment `preprod`, use os mesmos nomes de variável do `.env.example` com os valores daquele environment.
- **Production** fica só no environment `prod`. Não copie esses valores para `.env`, `.env.example` ou `appsettings*.json`.

### Postgres local

O banco local vem do repositório [`domus-dev`](https://github.com/DanRantino/domus-dev). O Dev Container sobe esse Compose ao iniciar. Fora do container, na pasta do `domus-dev`:

```bash
docker compose up -d
```

| Campo | Valor (stack `domus-dev`) |
| --- | --- |
| Imagem | `postgres:16` |
| Host / porta | `127.0.0.1:5432` |
| Database / user / senha | `domus` / `domus` / `domus` |
| URL | `postgresql://domus:domus@127.0.0.1:5432/domus` (`DATABASE_URL` em [`.env.example`](.env.example)) |

Este repositório não publica um Postgres. Parar o stack ou apagar o volume (`docker compose down`, `docker compose down -v`) é no `domus-dev`.

## Configuração

O contrato de variáveis é o mesmo nos dois perfis de desenvolvimento. No local, copie [`.env.example`](.env.example) para `.env` (já vem com a URL do Postgres do `domus-dev`, CORS e o tenant Logto Cloud; preencha só AppId, AppSecret e o M2M). No Railway Development e em Production, defina as mesmas chaves no serviço — não neste repositório.

| Variável                                        | Descrição                                                                 |
| ----------------------------------------------- | ------------------------------------------------------------------------- |
| `Authentication__Authority`                     | Issuer OIDC Logto (`…/oidc`) — validação JWT Bearer. Local: `https://9vhnmt.logto.app/oidc` |
| `Authentication__Audience`                      | API resource / `aud` (JWT + `options.Resource` do SDK)                    |
| `Logto__Endpoint`                               | URL do tenant **com barra final** (`https://9vhnmt.logto.app/`). Não use `…/oidc` |
| `Logto__AppId`                                  | App ID do Traditional Web App no Console Logto Cloud (placeholder vazio no exemplo) |
| `Logto__AppSecret`                              | App secret do Traditional Web App (só na API, nunca no front; placeholder vazio no exemplo) |
| `DATABASE_URL` ou `ConnectionStrings__Database` | Postgres. Local: URL do `domus-dev`. Railway: referência privada do environment (`ConnectionStrings__Database` ganha se as duas existirem) |
| `Cors__Origins__0`                              | Origem **pública** do SPA (local: `https://web.domus.dev`; Railway: `https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}`) |
| `Resend__ApiKey`                                | API key do Resend para e-mail de convite. Vazio em Development só registra o e-mail no log |
| `Resend__From`                                  | Remetente verificado no Resend (`Nome <email@dominio>`)                   |
| `Invitations__FrontendOrigin`                   | Origem pública do SPA usada no link do convite (`https://web.domus.dev`) |

No Dev Container a API escuta em `PORT=5000` (`https://api.domus.dev` e os caminhos same-origin em `https://web.domus.dev`). O SPA é `https://web.domus.dev`.

### Console Logto (Traditional Web App)

O perfil local usa o tenant Logto Cloud `https://9vhnmt.logto.app/` (o Logto self-hosted foi removido). Crie ou reutilize o aplicativo **Traditional Web** (não SPA) nesse tenant, como no [tutorial MVC](https://docs.logto.io/pt-BR/quick-starts/dotnet-core/mvc). As redirect URIs locais continuam na origem do **front**, no app Cloud:

| Ambiente | Redirect URI | Post sign-out redirect URI |
| --- | --- | --- |
| Local | `https://web.domus.dev/Callback` | `https://web.domus.dev/SignedOutCallback` |
| Railway (`preprod` e `prod`) | `https://<domínio-público-do-front>/Callback` | `https://<domínio-público-do-front>/SignedOutCallback` |

`api.domus.dev` continua para Swagger e Bearer direto. O Caddy do front encaminha `/Callback`, `/SignedOutCallback`, `/auth/*` e `/api/*` para esta API.

O Railway CLI vem no Dev Container. Secrets de serviço (M2M, audience, `DATABASE_URL` de preprod/prod) ficam no Railway — não no `.env` versionado. Depois de `railway login` e `railway link` neste repositório (serviço `domus-back`):

```bash
railway variable list --service domus-back --environment preprod
railway variable list --service domus-back --environment preprod --kv

# Secret sem aparecer no histórico do shell
printf '%s' "$SECRET" | railway variable set DevelopmentSeed__ClientSecret --stdin --service domus-back --environment preprod
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

- `DevelopmentSeed__LogtoEndpoint` — tenant Logto Cloud (`https://9vhnmt.logto.app/`)
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

Logs da aplicação vão para **stdout** e aparecem no log do Railway. Em `Development` (local e `preprod`) o formato é console de uma linha; em `Production` é JSON. Não há stack de logging externo neste marco.

### Serviço

Configuração de build/deploy: [`railway.toml`](railway.toml) (Dockerfile + healthcheck `/health/live`).

O perfil **Railway Development** é o environment `preprod` do serviço `domus-back`. Production é o environment `prod` do mesmo serviço: `ASPNETCORE_ENVIRONMENT=Production` e os segredos ficam só lá.

No diretório deste repo (projeto já linkado):

```bash
railway link --service domus-back --environment preprod

railway variable set \
  ASPNETCORE_ENVIRONMENT=Development \
  Authentication__Authority='<issuer-oidc-do-preprod>' \
  Authentication__Audience='<api-resource>' \
  Logto__Endpoint='<endpoint-logto-com-barra-final>' \
  Logto__AppId='<traditional-web-app-id>' \
  Cors__Origins__0='https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}' \
  --service domus-back \
  --environment preprod

printf '%s' "$LOGTO_APP_SECRET" | railway variable set Logto__AppSecret --stdin --service domus-back --environment preprod

# DATABASE_URL privada do Postgres deste environment (não use a URL pública do TCP proxy)
railway variable set DATABASE_URL='${{Postgres.DATABASE_URL}}' --service domus-back --environment preprod

railway domain
railway up
```

Alternativa no dashboard: New Service → GitHub `DanRantino/domus-back` → o `Dockerfile` / `railway.toml` são detectados automaticamente.

### Variáveis do serviço `domus-back`

| Variável                    | Railway Development (`preprod`)                                                                   | Production (`prod`)                                      |
| --------------------------- | ------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`    | `Development`                                                                                     | `Production`                                             |
| `Authentication__Authority` | Issuer OIDC do tenant Logto desse environment                                                     | Issuer do tenant de prod (não copiar para arquivo local) |
| `Authentication__Audience`  | API resource / `aud`                                                                              | API resource de prod                                     |
| `Logto__Endpoint`           | Tenant Logto **com barra final**                                                                  | Tenant de prod                                           |
| `Logto__AppId`              | App ID do Traditional Web App desse environment                                                   | App ID de prod                                           |
| `Logto__AppSecret`          | App secret (stdin; não no bundle do front nem no git)                                             | App secret de prod                                       |
| `DATABASE_URL`              | Referência privada `${{Postgres.DATABASE_URL}}` desse environment                                 | Referência privada do `prod`                             |
| `Cors__Origins__0`          | Origem **pública** do SPA: `https://${{domus-front.RAILWAY_PUBLIC_DOMAIN}}` (não use DNS interno) | Origem pública do front de prod                          |

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
