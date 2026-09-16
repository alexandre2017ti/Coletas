#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Somente demonstração: publish atual sobre runtime local; não substituir build de produção.
# Mudança: docs/mudancas/2026-09-14-05-limpeza-cadastros-demo.md
test -f artifacts/registration-api/Coletas.Api.dll
while IFS= read -r entry; do
  case "$entry" in POSTGRES_PASSWORD=*) export POSTGRES_PASSWORD="${entry#*=}";; esac
done < <(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' coletas-preview-postgres-1)
while IFS= read -r entry; do
  case "$entry" in Jwt__SigningKey=*) export JWT_SIGNING_KEY="${entry#*=}";; esac
done < <(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' coletas-preview-api-1)
docker tag "$(docker inspect --format '{{.Image}}' coletas-preview-api-1)" coletas-local-runtime:cached
docker build --pull=false -t coletas-preview-api:latest -t coletas-preview-migrate:latest -f infra/api.cached.Dockerfile artifacts/registration-api
docker compose -p coletas-preview up --no-build -d api
curl --retry 20 --retry-all-errors --retry-delay 2 --max-time 5 --fail --silent --show-error http://127.0.0.1:5080/health/ready
