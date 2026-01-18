# Desafio Tecnico — Cadastro de Herois (Vue 3 + .NET 8 + CQRS/MediatR + SQL Server + Docker)

Este repositorio implementa um **CRUD de herois** com relacionamento **N:N** entre **Herois** e **Superpoderes**, conforme o desafio.

A solução foi construida para rodar **100% via containers Linux** (Docker Compose) tanto em desenvolvimento quanto em execução “prod-like”, e para ser confortavel de evoluir no VS Code.

> **Starter Kit:** veja `docs/STARTER_KIT.md`
> 
> **Decisões:** veja `docs/DECISIONS.md`
> 
> **Contribuindo:** veja `CONTRIBUTING.md`

---

## Stack

### Backend
- .NET 8 (ASP.NET Core Web API)
- **CQRS** com **MediatR** (Commands/Queries + Handlers)
- **EF Core** + **SQL Server**
- **FluentValidation** (validação de Commands/Queries) + MediatR ValidationBehavior
- **ProblemDetails** (RFC 7807) para padronização de erros
- **RowVersion** (concorrência otimista)
- **Health checks** (`/health`)
- **Correlation ID** (`X-Correlation-ID`)

### Frontend
- Vue 3 + Vite + TypeScript
- Validade do formulario com **VeeValidate + Zod**
- Componentização (tela, lista, formulário, multiselect pesquisável, modal, toast)

### Infra / Execução
- Docker Compose (SQL Server + API + Frontend)

---

## Funcionalidades entregues

### Backend (API)
- CRUD completo de **Herois**
  - `POST /api/herois` (Create)
  - `GET /api/herois` (Read - lista paginada + busca)
  - `GET /api/herois/{id}` (Read - por id)
  - `PUT /api/herois/{id}` (Update - requer rowVersion)
  - `DELETE /api/herois/{id}` (Delete)
- Endpoint de consulta de **Superpoderes** (somente leitura)
  - `GET /api/superpoderes`
- **Seed**: 100 herois (Marvel) + seed ampliado de superpoderes

### Frontend (UI)
- Listagem de herois com:
  - **busca** por nome/nome de heroi
  - **paginação numerada** (com “...” quando necessario)
  - exibição de superpoderes como **badges** (somente nome)
- Inclusao/Edição em **modal (pop-up)** com indicação clara do modo (inclusao vs edição)
- Superpoderes em **multi-select pesquisavel** (chips + busca, UX inspirado no CoreUI)
- **Toast** de feedback para inclusao/alteração/remoção (sucesso/erro)
- Exclusao com **confirmação** e **delete otimista** (rollback em caso de erro)

---

## Regras do desafio implementadas

- **Id** gerado automaticamente.
- **NomeHeroi** nao pode repetir.
  - API retorna **409 Conflict** com mensagem tratada.
- **Id invalido/inexistente** em consulta/alteração/exclusao:
  - invalido → **400 Bad Request** (validação)
  - inexistente → **404 Not Found**
- **Listagem vazia** (`GET /api/herois`):
  - decisao do projeto: retorna **404 Not Found** com mensagem (comportamento mantido conforme combinado).
- **Delete** retorna **200 OK** com mensagem.

---

## Benfeitorias tecnicas implementadas

### 1) CQRS + MediatR
- Separação clara entre **Commands** (escrita) e **Queries** (leitura)
- Controllers “finos”: apenas validam entrada e delegam ao MediatR

### 2) MediatR Pipeline Behaviors
- **LoggingBehavior**: loga inicio/fim de cada request do MediatR
- **ValidationBehavior**: executa FluentValidation antes do Handler
- **TransactionBehavior**: abre transação automaticamente para operacoes de escrita

### 3) Validação com FluentValidation
- Validators para Commands/Queries (ex.: campos obrigatorios, ranges, paginação)
- Erros de validação retornam **ValidationProblemDetails (400)**

### 4) Erros padronizados com ProblemDetails (RFC 7807)
- Falhas retornam `ProblemDetails`/`ValidationProblemDetails`
- Inclui `traceId` e `correlationId` (quando aplicavel)

### 5) Paginação + busca no backend
`GET /api/herois` suporta:
- `page` (default 1)
- `pageSize` (default 20)
- `search` (opcional: procura em `Nome` e `NomeHeroi`)

Retorno (quando houver dados): `Result<PagedResult<HeroiDto>>`.

### 6) Concorrencia otimistica (RowVersion)
- A entidade `Heroi` possui `RowVersion`
- `PUT /api/herois/{id}` **requer** `rowVersion` no body (base64) para evitar sobrescrita concorrente
- Em caso de conflito: **409 Conflict**

### 7) Cache simples para Superpoderes
- `GET /api/superpoderes` usa `IMemoryCache` (TTL curto)

### 8) Observabilidade / Correlation ID
- Middleware gera/propaga `X-Correlation-ID`

### 9) Health checks
- Endpoint: `GET /health`
- Docker Compose aguarda o banco e a API ficarem saudaveis antes de subir o frontend

### 10) Globalization no Docker
- Configurado `<InvariantGlobalization>false</InvariantGlobalization>` para evitar problemas de ICU/cultura em Linux

---

## Decisoes tomadas durante a jornada (resumo)

> Versao completa em `docs/DECISIONS.md`.

- **Entidades do dominio com nomes “limpos”** (ex.: `Heroi`, `Superpoder`), evitando sufixos como `Entity`.
  - DTOs/Requests usam sufixos (`HeroiDto`, `CreateHeroRequest`) para nao colidir.
- **Superpoder.Nome** (C#) mapeado para coluna **Superpoder** (SQL).
  - Motivo: C# nao permite membro com o mesmo nome do tipo (`CS0542`).
- **Swagger** condicionado a `Development` por boas praticas; o Docker Compose define `ASPNETCORE_ENVIRONMENT=Development`.
- **HealthChecks EF** exigem o pacote `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.
- Seed (100 herois) **idempotente**: roda apenas se a tabela estiver vazia.

---

## Como rodar (Docker Compose)

### Pre-requisitos
- Docker + Docker Compose

### Subir o ambiente
Na raiz do projeto:

```bash
# opcional (recomendado): defina uma senha forte
export MSSQL_SA_PASSWORD='Your_strong_Passw0rd'

# sobe: SQL Server + API + Frontend
docker compose up --build

# (alternativa) usando scripts
./scripts/dev-up.sh
```

### Acessos
- Frontend: http://localhost:5173
- Swagger: http://localhost:8080/swagger
- Health: http://localhost:8080/health

### Variaveis uteis
- `MSSQL_SA_PASSWORD` — senha do usuario `sa` do SQL Server
- `SEED_MARVEL_HEROES=true` — forca seed de 100 herois mesmo fora de `Development`

---

## Endpoints (resumo)

- `GET /api/herois?page=1&pageSize=10&search=bat`
- `GET /api/herois/{id}`
- `POST /api/herois`
- `PUT /api/herois/{id}` (requer `rowVersion`)
- `DELETE /api/herois/{id}`
- `GET /api/superpoderes`

### Observação sobre `rowVersion` no update
Para atualizar um heroi:
1) faca `GET /api/herois/{id}`
2) use o `rowVersion` retornado no body do `PUT`

---

## Testes (backend)

### Unit tests (Handlers/CQRS)

```bash
cd backend
dotnet test src/Herois.Tests/Herois.Tests.csproj

# (alternativa) usando script
./scripts/test.sh
```

### Integration tests (API)

Os testes de integração usam `WebApplicationFactory` com **SQLite in-memory** (nao precisam subir SQL Server).

```bash
cd backend
dotnet test src/Herois.Api.IntegrationTests/Herois.Api.IntegrationTests.csproj
```

---

## Estrutura do repositorio

### Backend
- `backend/src/Herois.Api` — API (controllers, pipeline, DI)
- `backend/src/Herois.Application` — CQRS (commands/queries/handlers, DTOs, behaviors, validators)
- `backend/src/Herois.Domain` — entidades e regras do dominio
- `backend/src/Herois.Infrastructure` — EF Core (DbContext, mapeamentos)
- `backend/src/Herois.Tests` — unit tests dos handlers
- `backend/src/Herois.Api.IntegrationTests` — integration tests dos endpoints

### Frontend
- `frontend/src/views` — telas (ex.: `HeroesView.vue`)
- `frontend/src/components` — componentes (lista, formulario, multiselect, modal, toast)
- `frontend/src/services` — client HTTP
- `frontend/src/types` — tipagens (DTOs, ProblemDetails, paginação)
- `frontend/src/assets/styles` — CSS global

---

## Starter Kit do desenvolvedor

- `docs/STARTER_KIT.md` (comandos, VS Code, scripts e fluxo de contribuição)
- `.env.example` (variaveis prontas)
- `scripts/` (atalhos para subir, desligar, resetar DB e rodar testes)
- `.vscode/` (tasks + launch + sugestoes de extensoes)
- `CONTRIBUTING.md` + `.github/PULL_REQUEST_TEMPLATE.md` (padrao de contribuição e checklist)

---

## Troubleshooting (problemas comuns)

### Swagger retornando 404
O Swagger esta habilitado apenas em `Development`.
No Docker Compose ja definimos `ASPNETCORE_ENVIRONMENT=Development` no servico da API.

### Erro “Invalid column name 'RowVersion'”
Indica banco antigo sem a coluna.
Em dev, o mais simples e recriar o volume:

```bash
./scripts/reset-db.sh
```

---

## Proximos passos (opcionais)
- CI (GitHub Actions) para build + testes
- Export/Import de dados
- UI refinada (theme, responsividade, acessibilidade avancada)
