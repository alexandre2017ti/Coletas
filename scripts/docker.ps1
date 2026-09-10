param([Parameter(ValueFromRemainingArguments = $true)][string[]]$DockerArguments)
$ErrorActionPreference = "Stop"
# Motivo: usar o Docker Linux do projeto pelo PowerShell, sem depender de Docker Desktop.
# Mudança: docs/mudancas/2026-09-09-03-validacao-integrada.md
$taskRoot = Split-Path -Parent $PSScriptRoot
& wsl.exe -d coletas-dev -u root --cd $taskRoot -- docker @DockerArguments
exit $LASTEXITCODE
