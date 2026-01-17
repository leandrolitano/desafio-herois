#!/usr/bin/env bash
set -euo pipefail

service=${1:-}
if [ -z "$service" ]; then
  echo "Uso: scripts/logs.sh <service>"
  echo "Ex.: scripts/logs.sh api"
  exit 1
fi

docker compose logs -f --tail=200 "$service"
