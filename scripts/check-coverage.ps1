param([string]$ResultsDirectory = "tests/Coletas.Tests/TestResults", [double]$Minimum = 0.8)
$ErrorActionPreference = "Stop"
# Motivo: restringir regressões na base escrita à mão; geradores são excluídos no runsettings.
# Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
$taskReport = Get-ChildItem -LiteralPath $ResultsDirectory -Recurse -Filter coverage.cobertura.xml |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $taskReport) { throw "Relatório de cobertura não encontrado." }
[xml]$taskXml = Get-Content -LiteralPath $taskReport.FullName
$taskRate = [double]::Parse($taskXml.coverage.'line-rate', [Globalization.CultureInfo]::InvariantCulture)
Write-Output ("Cobertura: {0:P2}" -f $taskRate)
if ($taskRate -lt $Minimum) { throw "Cobertura inferior ao mínimo exigido." }
