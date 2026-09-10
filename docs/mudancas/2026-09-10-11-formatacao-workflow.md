# Registro da correção de formatação do workflow

## Objetivo e motivo
Registrar explicitamente os commits de formatação exigidos pelo `dotnet format`, pois a regra restritiva do projeto exige um documento novo para cada alteração versionada.

## Escopo e arquivos afetados
Este registro acompanha a formatação aplicada em `src/Coletas.Api/Configuration/AuthenticationConfiguration.cs` e `src/Coletas.Api/Configuration/RateLimitingConfiguration.cs`. Nenhuma regra de negócio foi alterada.

## Impacto
Somente whitespace e linha em branco foram ajustados para o `dotnet format --verify-no-changes` do CI. A execução anterior confirmou 40 testes locais aprovados.

## Validação
O workflow anterior falhou no `Documentation gate` por ausência deste registro, antes das etapas de compilação e testes. Após este registro, executar `dotnet format --verify-no-changes --no-restore`, `dotnet test` e acompanhar o workflow no GitHub.

## Migrações
Não aplicável; não houve alteração de modelo ou banco.

## Rollback
Reverter o commit deste registro e os dois commits de formatação, se necessário. Não reverter o commit funcional `5400542`.

## Pendências
Confirmar a conclusão bem-sucedida do workflow e configurar a proteção da branch com o check `Foundation checks`.
