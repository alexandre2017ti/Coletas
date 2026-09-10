# Cobertura de identidade e tarifas

## Motivo e objetivo

Ampliar testes comportamentais até pelo menos 90% das linhas próprias do backend, sem excluir código de produção da medição.

## Escopo

Testes HTTP com autenticação JWT real e banco EF isolado em memória; testes de domínio tarifário. O provedor em memória não verifica constraints PostgreSQL ou concorrência real.

## Arquivos afetados

tests/Coletas.Tests, lockfiles, scripts/check-coverage.ps1 e docs/PLANO-DO-PROJETO.md.

## Validação

Em execução. Não equivale a detectar 90% de todos os erros. Achados de revisão continuam pendentes quando não corrigidos explicitamente.

## Migrações e impacto

Sem migration. Dependência de banco em memória somente no projeto de testes.

## Rollback

Reverter os arquivos desta mudança; nenhum dado persistente alterado.
