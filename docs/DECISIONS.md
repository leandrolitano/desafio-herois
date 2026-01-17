# Decisoes e trade-offs

Este documento registra as principais decisoes tecnicas tomadas ao longo do desenvolvimento do desafio, com justificativa e alternativas.

## 1) CQRS + MediatR

**Decisao:** separar leitura e escrita (Queries vs Commands) usando MediatR.

**Por que:**
- organiza o codigo (controllers finos, handlers focados)
- melhora testabilidade (unit tests diretamente nos handlers)
- facilita evolucao (ex.: incluir logs, validacao e transacoes como behaviors)

**Alternativas consideradas:**
- controllers com services (camada de aplicacao unica)
- Minimal API

## 2) Behaviors (Logging/Validation/Transaction)

**Decisao:** centralizar cross-cutting concerns no pipeline do MediatR.

**Por que:** evita duplicacao de codigo e garante padrao.

## 3) FluentValidation

**Decisao:** validar entrada (commands/queries) com FluentValidation.

**Por que:** regras ficam declarativas e reutilizaveis; erros viram `ValidationProblemDetails`.

## 4) Padrao de erro com ProblemDetails (RFC 7807)

**Decisao:** retornar erros como `ProblemDetails`/`ValidationProblemDetails`.

**Por que:** front consegue tratar mensagens de forma previsivel e padronizada.

## 5) Listagem vazia retorna 404

**Decisao:** manter `GET /api/herois` retornando `404` quando nao houver registros.

**Por que:** foi uma decisao combinada para atender ao enunciado e exercitar tratativa de erro no frontend.

**Alternativas:** `200 []` (mais comum em APIs), ou `204 NoContent`.

## 6) Concorrencia otimista com RowVersion

**Decisao:** incluir `RowVersion` (SQL Server rowversion) e exigir no update.

**Por que:** evita sobrescrever alteracoes concorrentes e demonstra maturidade de API.

**Observacao:** se o banco ja existia, pode faltar a coluna. Em DEV, o reset do volume resolve. Para facilitar o desafio, existe um hotfix de compatibilidade.

## 7) Paginação + busca

**Decisao:** implementar paginação e filtro no backend e refletir no frontend.

**Por que:** com seed de 100 herois, e importante para UX e performance.

## 8) Cache de superpoderes

**Decisao:** cache simples (IMemoryCache) para `/api/superpoderes`.

**Por que:** lista muda pouco; reduz carga no banco.

## 9) Infra via Docker Compose

**Decisao:** SQL Server + API + Front em containers e uma experiancia 100% Linux.

**Por que:** atende ao requisito do desafio e padroniza execucao.

## 10) Swagger apenas em Development

**Decisao:** manter swagger condicionado ao ambiente e configurar o compose com `ASPNETCORE_ENVIRONMENT=Development`.

**Por que:** bom habito de seguranca; e para o desafio o compose ja deixa ligado.

## 11) Tests (unit + integration)

**Decisao:**
- Unit tests em handlers (rapidos)
- Integration tests da API com WebApplicationFactory e SQLite in-memory (nao dependem do SQL Server)

**Por que:** cobre logica de negocio e pipeline HTTP sem custo de infra.

## 12) Frontend: modal + multiselect pesquisavel + badges

**Decisao:**
- Form em modal para deixar claro quando esta editando
- Multi select com busca para lidar com lista extensa
- Badges na tabela para legibilidade


## 13) Globalization no Linux

**Decisao:** manter `<InvariantGlobalization>false</InvariantGlobalization>`.

**Por que:** em alguns ambientes Linux (containers) a aplicacao pode falhar ou apresentar comportamentos inesperados com culturas/ICU quando a globalizacao fica invariante.

## 14) Consistencia de contratos (DTOs vs Entidades)

**Decisao:** expor DTOs com nomes amigaveis e consistentes com o frontend.

**Exemplos:**
- `SuperpoderDto.Nome` no JSON (e nao `superpoder`) para ficar alinhado com o uso no Vue.
- `Superpoder.Nome` (C#) mapeado para coluna `Superpoder` no banco para evitar o erro CS0542.

## 15) Swagger como ferramenta de desenvolvimento

**Decisao:** Swagger habilitado apenas em `Development`; o docker-compose seta `ASPNETCORE_ENVIRONMENT=Development` para o desafio.

**Por que:** evita expor documentacao de forma indiscriminada em ambientes produtivos.
