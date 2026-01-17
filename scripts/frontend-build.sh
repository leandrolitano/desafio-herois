#!/usr/bin/env bash
set -euo pipefail

echo "[frontend-build] Instalando dependencias e buildando..."
(
  cd frontend
  npm ci
  npm run build
)

echo "[frontend-build] OK"
