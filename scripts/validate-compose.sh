#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
# Motivo: senha efêmera em memória e namespace exclusivo isolam a verificação dos dados de desenvolvimento.
# Mudança: docs/mudancas/2026-09-09-03-validacao-integrada.md
export POSTGRES_PASSWORD
POSTGRES_PASSWORD="$(openssl rand -hex 24)"
# Motivo: autenticação exige chave mesmo no aceite; segredo existe só nesta execução.
# Mudança: docs/mudancas/2026-09-10-04-revalidacao-fundacao.md
export JWT_SIGNING_KEY
JWT_SIGNING_KEY="$(openssl rand -hex 32)"
expected_migrations="$(find src/Coletas.Infrastructure/Persistence/Migrations -maxdepth 1 -type f -name '[0-9]*.cs' ! -name '*.Designer.cs' | wc -l)"
export COMPOSE_PROJECT_NAME="coletas-validation-$(date +%s)-$$"
compose() { docker compose -p "$COMPOSE_PROJECT_NAME" "$@"; }
cleanup() {
  # Somente volumes criados nesta execução descartável; nunca usar o namespace coletas-dev.
  compose down --volumes --remove-orphans
}
trap cleanup EXIT
compose config --quiet
compose up --build --quiet-pull -d
curl --retry 30 --retry-all-errors --retry-delay 2 --max-time 15 --fail --silent --show-error http://localhost:5080/health/ready
printf '\n'
curl --fail --silent --show-error http://localhost:5080/api/v1/platform
printf '\n'
curl --retry 20 --retry-all-errors --retry-delay 2 --fail --silent --show-error http://localhost:5173/health/ready
printf '\n'
test "$(compose exec -T redis redis-cli ping)" = PONG
test "$(compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT count(*) FROM "__EFMigrationsHistory";')" -eq "$expected_migrations"
test "$(compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT "Value" FROM configuration."SystemSettings" WHERE "Key" = '\''DeliveryGrouping.RadiusMeters'\'';')" = 5000
compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT postgis_version();'
# A consulta espacial exercita distância em metros, não apenas instalação da extensão.
test "$(compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT ST_DWithin(ST_SetSRID(ST_MakePoint(-56.1,-15.6),4326)::geography,ST_SetSRID(ST_MakePoint(-56.1,-15.6),4326)::geography,5000);')" = t
test "$(compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT ST_DWithin(ST_SetSRID(ST_MakePoint(-56.1,-15.6),4326)::geography,ST_SetSRID(ST_MakePoint(-55.1,-15.6),4326)::geography,5000);')" = f
compose run --rm migrate
test "$(compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT count(*) FROM "__EFMigrationsHistory";')" -eq "$expected_migrations"
test "$(compose exec -T postgres psql -U coletas -d coletas -Atc 'SELECT count(*) FROM configuration."SystemSettings";')" = 1
compose restart api
curl --retry 15 --retry-all-errors --retry-delay 2 --fail --silent --show-error http://localhost:5080/health/ready
# Motivo: readiness deve detectar uma dependência caída e recuperar sem recriar a API.
compose stop redis
test "$(curl --max-time 20 --silent --output /dev/null --write-out '%{http_code}' http://localhost:5080/health/ready)" = 503
test "$(curl --max-time 10 --silent --output /dev/null --write-out '%{http_code}' http://localhost:5080/health/live)" = 200
compose start redis
curl --retry 20 --retry-all-errors --retry-delay 2 --max-time 15 --fail --silent --show-error http://localhost:5080/health/ready
printf '\nVALIDATION PASSED: migration, seed, PostGIS, Redis, API, proxy web e reinício.\n'
