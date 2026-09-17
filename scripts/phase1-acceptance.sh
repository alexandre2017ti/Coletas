#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Ambiente isolado: estes nomes nunca apontam para o banco coletas da demonstração.
# Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
name=coletas-phase1-acceptance
database=coletas_phase1_acceptance
if [[ "${1:-}" == stop ]]; then
  docker rm -f "$name" >/dev/null
  docker exec coletas-preview-postgres-1 dropdb -U coletas "$database"
  echo "Ambiente e dados fictícios de aceite removidos."
  exit
fi
test -f artifacts/registration-api/Coletas.Api.dll
if docker inspect "$name" >/dev/null 2>&1; then echo "Ambiente de aceite já existe; não sobrescrever." >&2; exit 1; fi
docker exec coletas-preview-postgres-1 createdb -U coletas "$database"
image=$(docker inspect --format '{{.Image}}' coletas-preview-api-1)
env_args=()
while IFS= read -r entry; do
  case "$entry" in
    Infrastructure__Postgres=*) env_args+=(--env "${entry/Database=coletas/Database=$database}");;
    Infrastructure__Redis=*|Jwt__*|ASPNETCORE_ENVIRONMENT=*) env_args+=(--env "$entry");;
  esac
done < <(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' coletas-preview-api-1)
env_args+=(--env Logging__LogLevel__Default=Warning --env PrivateDocuments__RootPath=/tmp/documents)
docker run --rm --network coletas-preview_default "${env_args[@]}" -v "$PWD/artifacts/registration-api:/validation:ro" -w /validation --entrypoint dotnet "$image" Coletas.Api.dll --migrate
docker run -d --name "$name" --network coletas-preview_default "${env_args[@]}" --tmpfs /tmp/documents:rw,noexec,nosuid,mode=1777,size=64m -p 127.0.0.1:5081:8080 -v "$PWD/artifacts/registration-api:/validation:ro" -w /validation --entrypoint dotnet "$image" Coletas.Api.dll >/dev/null
curl --retry 15 --retry-all-errors --retry-delay 2 --max-time 5 --fail --silent http://127.0.0.1:5081/health/ready
echo " Aceite isolado disponível em 5081."
