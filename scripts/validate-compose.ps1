$ErrorActionPreference = "Stop"
$taskRoot = Split-Path -Parent $PSScriptRoot
# Motivo: o mesmo roteiro deve rodar no Ubuntu/CI e no WSL pelo PowerShell.
# Mudança: docs/mudancas/2026-09-09-03-validacao-integrada.md
& wsl.exe -d coletas-dev -u root --cd $taskRoot -- bash scripts/validate-compose.sh
if ($LASTEXITCODE -ne 0) { throw "Validação integrada falhou (exit $LASTEXITCODE)." }
