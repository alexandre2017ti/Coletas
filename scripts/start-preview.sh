#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

# Motivo: reutilizar as credenciais do banco local existente sem imprimir segredos
# ou gerar uma senha incompatível com o volume persistente. Não usar em produção.
# Mudança: docs/mudancas/2026-09-10-16-integracao-fase-1.md
project=coletas-preview
if docker container inspect "$project-postgres-1" >/dev/null 2>&1; then
  while IFS= read -r entry; do
    case "$entry" in POSTGRES_PASSWORD=*) export POSTGRES_PASSWORD="${entry#*=}" ;; esac
  done < <(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' "$project-postgres-1")
elif docker volume inspect "${project}_postgres-data" >/dev/null 2>&1; then
  echo 'Volume de demonstração existe sem container de referência. Configure POSTGRES_PASSWORD no ambiente para reutilizá-lo.' >&2
  : "${POSTGRES_PASSWORD:?Senha do banco local necessária}"
else
  export POSTGRES_PASSWORD="$(openssl rand -hex 24)"
fi

if docker container inspect "$project-api-1" >/dev/null 2>&1; then
  while IFS= read -r entry; do
    case "$entry" in Jwt__SigningKey=*) export JWT_SIGNING_KEY="${entry#*=}" ;; esac
  done < <(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' "$project-api-1")
fi
export JWT_SIGNING_KEY="${JWT_SIGNING_KEY:-$(openssl rand -hex 32)}"
docker compose -p "$project" up --build -d api
curl --retry 20 --retry-all-errors --retry-delay 2 --max-time 10 --fail --silent --show-error http://localhost:5080/health/ready
printf '\nAPI local pronta: http://localhost:5080. Site Vite: http://127.0.0.1:5173\n'
