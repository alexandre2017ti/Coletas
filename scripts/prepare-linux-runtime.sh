#!/usr/bin/env bash
set -euo pipefail
# Motivo: fornecer engine Linux reproduzível dentro do WSL exclusivo do projeto.
# Mudança: docs/mudancas/2026-09-09-03-validacao-integrada.md
if [ "$(id -u)" != 0 ]; then
  echo "Execute como root no Ubuntu reservado ao Coletas." >&2
  exit 1
fi
. /etc/os-release
test "$ID" = ubuntu
test "$VERSION_ID" = 24.04
apt-get update -qq
DEBIAN_FRONTEND=noninteractive apt-get install -y -qq docker.io docker-compose-v2 docker-buildx ca-certificates curl openssl
service docker start
docker version
docker compose version
