# Desafio Técnico — Cadastro de Heróis (Vue 3 + .NET 8 + CQRS/MediatR + SQL Server + Docker)

Este repositório implementa o desafio de **CRUD de heróis** com associação N:N de **Superpoderes**, usando:

- **Backend:** .NET 8 (Web API), **CQRS com MediatR**, EF Core
- **Frontend:** Vue 3 + Vite + TypeScript
- **Banco:** SQL Server (Docker)
- **Execução:** Docker Compose (Linux containers)

## Como rodar (Docker)

1) Tenha **Docker** e **Docker Compose** instalados.

2) Na raiz do projeto, suba tudo:

```bash
# opcional (recomendado): defina uma senha forte
export MSSQL_SA_PASSWORD='Your_strong_Passw0rd'

docker compose up --build
```

3) Acessos:
- Frontend: http://localhost:5173
- Swagger: http://localhost:8080/swagger

> Observação: o backend aplica **migrations automaticamente** na inicialização (para fins do desafio) e faz seed de superpoderes na primeira execução.
>
> Observação 2: o backend está com `<InvariantGlobalization>false</InvariantGlobalization>` para evitar erros de cultura/ICU em execução Linux (Docker).

## Endpoints

- `GET /api/herois?page=1&pageSize=10&search=bat`
- `GET /api/herois/{id}`
- `POST /api/herois`
- `PUT /api/herois/{id}` (**requer** `RowVersion` no body para concorrencia otimista)
- `DELETE /api/herois/{id}`
- `GET /api/superpoderes` (somente leitura)

## Regras do desafio implementadas

- **Id** gerado automaticamente.
- **NomeHeroi** não pode repetir (**retorna 409 Conflict**).
- **Id inexistente**: retorna **404 Not Found** com mensagem.
- **Listagem vazia**: comportamento atual é **404 Not Found** com mensagem (podemos ajustar para 204 ou 200+[] se preferir).
- Delete: retorna **200 OK** com mensagem.

### Paginacao e filtro

`GET /api/herois` suporta:

- `page` (default 1)
- `pageSize` (default 20)
- `search` (opcional; procura por `Nome` ou `NomeHeroi`)

O retorno (quando houver dados) e um `Result<PagedResult<HeroiDto>>`.

### Concorrrencia otimista (RowVersion)

Para atualizar (`PUT /api/herois/{id}`), envie o `RowVersion` recebido ao criar/buscar o heroi.
Se o registro tiver sido alterado por outro processo, a API retorna **409 Conflict**.

## Estrutura (backend)

- `Application/` — Commands/Queries/Handlers (CQRS)
- `Domain/` — Entidades
- `Infrastructure/` — DbContext (EF Core)
- `Api/` — Controllers

## Estrutura (frontend)

- `views/` — Telas (ex.: `HeroesView.vue`)
- `components/heroes/` — Componentes de UI (lista, formulário)
- `services/` — Client HTTP/API
- `assets/styles/` — CSS global (base/layout/componentes)

## Scripts SQL

Em `infra/sql/init.sql` há um script opcional de criação/seed (caso você queira usar abordagem SQL-first). O projeto atual usa EF Code-first.


## Testes (backend)

### Unit tests (Handlers / CQRS)

Na pasta `backend/`:

```bash
cd backend
dotnet test src/Herois.Tests/Herois.Tests.csproj
```

### Integration tests (API endpoints)

Os testes de integração usam `WebApplicationFactory` com **SQLite in-memory** (não precisam subir SQL Server).

```bash
cd backend
dotnet test src/Herois.Api.IntegrationTests/Herois.Api.IntegrationTests.csproj
```

> Observação: mantemos o comportamento atual da lista vazia (**GET /api/herois** retorna **404** com mensagem).

## UX (frontend)

- Lista e formulário separados em componentes
- Listagem com **paginacao numerada** e filtro (search)
- Formulario com validacao via **VeeValidate + Zod**
- Exclusão pede confirmação (modal)
- Operações exibem feedback via toast (sucesso/erro)
- Validacao de formulario com **VeeValidate + Zod**
- Paginacao com botoes numerados (e busca)

### Melhorias adicionais
- Formulário de inclusão/edição em **modal (pop-up)**, com indicação clara de **modo edição/inclusão**.
- Campo de superpoderes com **multi-select pesquisável** (inspirado no CoreUI Multi Select), exibindo os selecionados como "chips".
- Na tabela, superpoderes exibidos como **labels/badges** (somente o nome, sem descrição).
