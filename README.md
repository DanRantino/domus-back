# Domus API

Backend .NET da Domus. Monólito modular: **Domain / Application / Infrastructure / Api**. Capability inicial: **Users** + JWT Logto + `GET/POST /me`.

## Requisitos

- .NET SDK 10.0.302
- PostgreSQL 16 (Compose local neste repo)
- Railway CLI autenticada (`domus-api` nos environments `local` e `development`)
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

## Perfis de ambiente

Um único modelo de aplicação. A seleção de perfil é configuração (Railway + arquivos locais), não ramificação de código.

Neste repositório o serviço Railway é **`domus-api`**. O frontend correspondente é **`domus-web`**.

| Perfil Railway | Onde a API roda | Postgres | Como obter as variáveis |
| -------------- | --------------- | -------- | ----------------------- |
| `local` | máquina do desenvolvedor | Docker Compose deste repo | `railway variable list --service domus-api --environment local --kv > .env` |
| `development` | Railway Development | Postgres do environment `development` | `railway variable list --service domus-api --environment development --kv > .env` |

Production é um environment Railway separado e fica fora deste fluxo.

### Postgres local

```bash
docker compose -f compose.yaml up -d
```

| Campo | Valor |
| ----- | ----- |
| Imagem | `postgres:16` |
| Host / porta | `127.0.0.1:5432` |
| Database / user / password | `domus` / `domus` / `domus` |
| URL | `postgresql://domus:domus@127.0.0.1:5432/domus` |

Volume nomeado: `domus-postgres-data`. Parar: `docker compose -f compose.yaml down`. Apagar estado: `docker compose -f compose.yaml down -v`.

### Puxar `.env` do Railway

Na raiz deste repo (já autenticado na CLI):

```bash
railway link --project <projeto> --environment local --service domus-api
railway variable list --service domus-api --environment local --kv > .env
cp .env.local.example .env.local
```

Para o perfil hospedado, troque `--environment development`. Não commite `.env` nem `.env.local`.

[`.env.example`](.env.example) lista só os nomes das chaves. Não copie valores de production/preprod para o repositório.

A API carrega `.env` e depois `.env.local` (este último ganha nas chaves que ainda não estavam no processo). Variáveis já definidas no ambiente do processo não são sobrescritas. `.env.local` é o override deliberado do Postgres Docker sobre o `DATABASE_URL` puxado do Railway.

Sem gravar arquivo:

```bash
railway run --service domus-api --environment local -- dotnet run --project src/Domus.Api
```

| Variável | Descrição |
| -------- | --------- |
| `Authentication__Authority` | Issuer OIDC Logto (`…/oidc`) |
| `Authentication__Audience` | API resource / `aud` (mesmo valor que `VITE_LOGTO_API_RESOURCE` no `domus-web`) |
| `DATABASE_URL` ou `ConnectionStrings__Database` | Postgres. No perfil `local`, sobrescrever com a URL do Compose |
| `Cors__Origins__0` | Origem do SPA (default: `http://localhost:3000`) |
| `DevelopmentSeed__*` | M2M Logto usado só por `--seed` |

Porta local: `http://localhost:3001`. No Railway a app lê `PORT` e escuta em `0.0.0.0:$PORT`. Não defina `ASPNETCORE_URLS=http://localhost:3001` no environment `development`.

## Executar

```bash
dotnet run --project src/Domus.Api
```

Migrations rodam automaticamente no startup quando o provider é PostgreSQL.

Se precisar aplicar migrations manualmente:

```bash
ConnectionStrings__Database='<connection-string>' \
dotnet ef database update \
  --project src/Domus.Infrastructure \
  --startup-project src/Domus.Api
```

## Seed de desenvolvimento

Garante usuários no Logto, os mesmos usuários no Postgres, as casas e os vínculos (`house_memberships`). Não sobe o servidor HTTP.

```bash
dotnet run --project src/Domus.Api -- --seed
```

O `--` entrega `--seed` para a aplicação. Além do banco (`DATABASE_URL` ou `ConnectionStrings__Database`), o comando precisa das variáveis M2M puxadas do Railway (`DevelopmentSeed__*`):

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
```

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

## Railway (`domus-api`)

Railpack não suporta .NET: o build usa o [`Dockerfile`](Dockerfile) na raiz (SDK/runtime **10.0**). [`railway.toml`](railway.toml) define Dockerfile + healthcheck `/health/live`.

Logs da aplicação vão para **stdout** (JSON console no environment hospedado) e aparecem no log do Railway.

O environment **`development`** é o perfil Railway Development. As variáveis já existem nesse environment; puxe-as com a CLI em vez de copiar secrets para o git.

```bash
railway link --project <projeto> --environment development --service domus-api
railway up
```

`DATABASE_URL` no `development` deve ser a referência privada do Postgres daquele environment (não a URL pública do TCP proxy). `ASPNETCORE_ENVIRONMENT` nesse perfil não é Production.

### Verificação

```bash
curl -s https://<api-domain>/health/live
curl -s https://<api-domain>/health/ready
curl -s -o /dev/null -w '%{http_code}\n' https://<api-domain>/me   # esperado: 401
```

No `domus-web`, aponte a base URL da API para o domínio correspondente e leia o envelope (`data` / `error.code`).

## Follow-up no frontend

O `domus-web` orquestra self-serve (`GET /me` → 403 → `POST /me` → `GET /me`) e precisa ler o envelope.
