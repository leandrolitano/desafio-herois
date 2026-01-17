#!/usr/bin/env bash
set -euo pipefail

echo "[dev-down] Derrubando ambiente (mantendo volumes)..."
docker compose down
