#!/usr/bin/env bash
set -euo pipefail

echo "[test] Rodando testes do backend (unit + integration)..."
(
  cd backend
  dotnet test src/Herois.Tests/Herois.Tests.csproj
  dotnet test src/Herois.Api.IntegrationTests/Herois.Api.IntegrationTests.csproj
)

echo "[test] OK"
