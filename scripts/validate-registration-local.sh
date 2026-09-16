#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Motivo: testar o publish atual com runtime Linux em cache quando o registry está indisponível.
# Mudança: docs/mudancas/2026-09-14-02-cadastro-postgres-real.md
test -f artifacts/registration-api/Coletas.Api.dll
docker inspect coletas-preview-postgres-1 >/dev/null
image=$(docker inspect --format '{{.Image}}' coletas-preview-api-1)
env_args=()
while IFS= read -r entry; do
  case "$entry" in Infrastructure__*|Jwt__*|ASPNETCORE_ENVIRONMENT=*) env_args+=(--env "$entry");; esac
done < <(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' coletas-preview-api-1)
name="coletas-registration-check-$(date +%s)-$$"
# Banco exclusivo desta execução permite validar índices mesmo com legado duplicado na demonstração.
# Mudança: docs/mudancas/2026-09-14-04-identificadores-exclusivos-entregador.md
export ACCEPTANCE_DB="registration_check_$(date +%s)_$$"
export ACCEPTANCE_CONTAINER="$name"
created_db=false
cleanup() {
  docker rm -f "$name" >/dev/null 2>&1 || true
  if $created_db; then docker exec coletas-preview-postgres-1 dropdb -U coletas "$ACCEPTANCE_DB"; fi
}
trap cleanup EXIT
docker exec coletas-preview-postgres-1 createdb -U coletas "$ACCEPTANCE_DB"
created_db=true
for i in "${!env_args[@]}"; do
  if [[ "${env_args[$i]}" == Infrastructure__Postgres=* ]]; then
    env_args[$i]="${env_args[$i]/Database=coletas/Database=$ACCEPTANCE_DB}"
  fi
done
# Migration revisada é aditiva; nenhum dado de demonstração é removido.
docker run --rm --network coletas-preview_default "${env_args[@]}" \
  -v "$PWD/artifacts/registration-api:/validation:ro" -w /validation \
  --entrypoint dotnet "$image" Coletas.Api.dll --migrate
docker run -d --name "$name" --network coletas-preview_default "${env_args[@]}" \
  -p 127.0.0.1:5081:8080 -v "$PWD/artifacts/registration-api:/validation:ro" -w /validation \
  --entrypoint dotnet "$image" Coletas.Api.dll >/dev/null
curl --retry 15 --retry-all-errors --retry-delay 2 --max-time 5 --fail --silent http://127.0.0.1:5081/health/ready
python3 scripts/validate-registration-local.py
python3 scripts/validate-registration-uniqueness.py
