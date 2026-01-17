#!/usr/bin/env bash
set -euo pipefail

echo "[reset-db] ATENCAO: isso remove volumes (dados) do SQL Server!"
echo "[reset-db] Derrubando containers e apagando volumes..."
docker compose down -v

echo "[reset-db] Subindo tudo novamente..."
docker compose up --build
