# Testes de casos de borda

## Objetivo e motivo
Exercitar os caminhos de validação de tarifa e respostas de identidade identificados no relatório de cobertura, mantendo asserções sobre comportamento observável.

## Escopo e arquivos afetados
Adicionado `tests/Coletas.Tests/EdgeCaseTests.cs`. Nenhum código de produção ou migration foi alterado.

## Impacto
Aumenta a cobertura de regras de tarifa e do mapeador de respostas. O teste do mapeador confirma criação da resposta, enquanto os testes de domínio verificam exceções e arredondamento.

## Validação
Executar `dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"` e o script de cobertura. Registrar números efetivamente obtidos após a execução.

## Migrações
Não aplicável; não houve alteração de banco.

## Rollback
Remover `tests/Coletas.Tests/EdgeCaseTests.cs` e este registro, preservando registros anteriores.

## Pendências
Fluxo de migrations com `--migrate` e claims inválidas dependem de testes de hospedagem mais específicos e permanecem para uma etapa posterior se a cobertura de branches exigir.
