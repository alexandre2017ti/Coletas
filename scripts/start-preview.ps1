$ErrorActionPreference = "Stop"
# Motivo: a demonstração persiste no Ubuntu WSL2 e reutiliza credenciais locais.
# Mudança: docs/mudancas/2026-09-10-16-integracao-fase-1.md
$taskRoot = Split-Path -Parent $PSScriptRoot
& wsl.exe -d coletas-dev -u root --cd $taskRoot -- bash scripts/start-preview.sh
exit $LASTEXITCODE
