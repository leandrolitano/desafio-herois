# Contributing

Obrigado por contribuir!

Este repositorio foi estruturado para ser facil de evoluir no VS Code e para rodar 100% via Docker Compose.

> Para um guia de execucao e comandos, veja: `docs/STARTER_KIT.md`.

---

## Fluxo recomendado

1) Crie uma branch a partir de `main`.

Sugestao de nomes:
- `feat/<resumo>` (nova funcionalidade)
- `fix/<resumo>` (correcao)
- `chore/<resumo>` (manutencao/refactor)
- `docs/<resumo>` (documentacao)

2) Faca commits pequenos e objetivos.

Recomendacao (estilo "Conventional Commits"):
- `feat: adiciona paginacao na listagem`
- `fix: corrige validacao de NomeHeroi`
- `docs: atualiza README`

3) Antes de abrir PR, rode os testes do backend:

```bash
./scripts/test.sh
```

4) Abra um Pull Request descrevendo:
- o problema/objetivo
- a solucao
- como testar
- possiveis trade-offs

---

## Checklist de PR (recomendado)

- [ ] `docker compose up --build` sobe sem erros
- [ ] Swagger abre em `http://localhost:8080/swagger`
- [ ] Frontend abre em `http://localhost:5173`
- [ ] Testes do backend passam (`./scripts/test.sh`)
- [ ] Mudancas em API foram refletidas no frontend (tipos/DTOs)
- [ ] Mensagens/erros seguem o padrao `ProblemDetails`
- [ ] README/docs atualizados quando necessario

---

## Padroes de codigo (backend)

### CQRS + MediatR

Para manter o padrao do projeto:

1) Crie um Command/Query em:
- `backend/src/Herois.Application/Herois/Commands`
- `backend/src/Herois.Application/Herois/Queries`

2) Crie o Handler em:
- `backend/src/Herois.Application/Herois/Handlers`

3) (Opcional/recomendado) Crie validator (FluentValidation) em:
- `backend/src/Herois.Application/Herois/Validators`

4) Exponha via controller em:
- `backend/src/Herois.Api/Controllers`

### Validacao

O `ValidationBehavior` executa os validators automaticamente.
Erros de validacao devem retornar `ValidationProblemDetails` (400).

### Transacoes

O `TransactionBehavior` abre transacao automaticamente para operacoes de escrita.
Evite abrir transacao manual dentro de handlers (salvo casos especiais).

### Concorrencia (RowVersion)

Updates exigem `rowVersion` no body para evitar sobrescrita concorrente.
Se houver conflito, retorne 409.

---

## Padroes de codigo (frontend)

- Componentes pequenos e focados:
  - `views/` controla estado e orquestracao
  - `components/` contem UI reutilizavel
- Contratos tipados em `src/types/`
- Tratamento de erros baseado em `ProblemDetails`
- Formulario com `VeeValidate + Zod`

---

## Como reportar bugs

Quando abrir uma issue, inclua:
- passo a passo para reproduzir
- logs relevantes (backend e/ou frontend)
- prints (se aplicavel)
- ambiente (OS, Docker version, etc.)
