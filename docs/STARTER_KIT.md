# Starter Kit do Desenvolvedor

Este guia e para quem vai clonar o repositorio e comecar a desenvolver com o minimo de friccao.

## Pre-requisitos

- Docker + Docker Compose
- .NET SDK 8 (apenas se voce quiser rodar testes/servicos fora do Docker)
- Node.js 18+ (apenas se voce quiser rodar o frontend fora do Docker)
- VS Code (recomendado)

## Quick start (modo recomendado: 100% Docker)

1) Na raiz do projeto, crie um `.env` (opcional) a partir do exemplo:

```bash
cp .env.example .env
```

2) Suba o ambiente:

```bash
./scripts/dev-up.sh
```

Acessos:
- Frontend: http://localhost:5173
- Swagger:  http://localhost:8080/swagger
- Health:   http://localhost:8080/health

## Reset rapido do banco (DEV)

> ATENCAO: isso apaga o volume do SQL Server.

```bash
./scripts/reset-db.sh
```

## Rodar testes (backend)

```bash
./scripts/test.sh
```

## VS Code (recomendado)

### Extensoes sugeridas
- C# Dev Kit (ms-dotnettools)
- Docker (ms-azuretools)
- Volar (Vue)
- ESLint
- EditorConfig

> O repositorio inclui `.vscode/extensions.json` para sugestao automatica.

### Tasks prontas
Abra o Command Palette (Ctrl+Shift+P) e rode:
- **Tasks: Run Task** -> `Docker: up (build)`
- **Tasks: Run Task** -> `Docker: down (remove volumes)`
- **Tasks: Run Task** -> `Backend: test (unit + integration)`

### Debug da API fora do Docker (opcional)

Se voce preferir debugar a API localmente (mantendo o SQL Server no Docker):

1) Suba apenas o banco:

```bash
docker compose up -d db
```

2) Rode o debug no VS Code:
- `Run and Debug` -> **.NET: API (local)**

> Esse profile usa `Server=localhost,1433` e `ASPNETCORE_ENVIRONMENT=Development`.

## Como adicionar funcionalidade seguindo o padrao CQRS

### 1) Criar um Command/Query
- Crie o arquivo em `backend/src/Herois.Application/Herois/Commands` ou `Queries`.
- Implemente `ICommand<T>` ou `IQuery<T>`.

### 2) Criar o Handler
- Em `Handlers/`, implemente `IRequestHandler<SeuCommand, Result<T>>`.

### 3) Validacao
- Crie um validator em `Validators/` (FluentValidation).
- O `ValidationBehavior` executa automaticamente.

### 4) Controller
- Crie/ajuste um endpoint no `Herois.Api` chamando `_mediator.Send(...)`.

### 5) Testes
- Unit: `backend/src/Herois.Tests`
- Integration: `backend/src/Herois.Api.IntegrationTests`

## Troubleshooting rapido

- Swagger 404: verifique `ASPNETCORE_ENVIRONMENT=Development` no compose.
- RowVersion inexistente: em DEV use `./scripts/reset-db.sh`.

## Contribuindo

Para padroes de branch/commit e checklist de PR, veja:
- `CONTRIBUTING.md`
- `.github/PULL_REQUEST_TEMPLATE.md`
