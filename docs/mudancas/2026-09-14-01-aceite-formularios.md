# Aceite dos formulários e máscara de placa

## Objetivo e motivo

Concluir a verificação dos ajustes de alinhamento, máscaras e consulta CNPJ. Seis testes anteriores expiravam ao procurar o WhatsApp porque seu nome acessível incluía a ajuda. O separador da placa também precisava ser restrito à posição após as três letras.

## Escopo e arquivos afetados

`apps/web/src/components/Registration.tsx`, `apps/web/src/registrationValidation.ts`, `apps/web/src/App.tsx`, `apps/web/tests/registration.spec.ts` e `docs/PLANO-DO-PROJETO.md`. Identificação acessível estável, regressão da edição da placa e correção da descrição que tratava cadastros como endereço inexistente; sem alteração de contrato HTTP.

## Testes e validação

18 testes de navegador aprovados (desktop e viewport mobile), incluindo bordas, alturas, máscaras, digitação/exclusão da placa, consulta simulada e envio simulado. Build web aprovado. Capturas dos formulários conferidas: campos alinhados e sem transbordamento horizontal. Esses resultados não comprovam persistência na API real nem consulta ao provedor externo em produção.

## Impacto e migrações

Sem migração. Sete caracteres alfanuméricos da placa continuam sendo enviados à API; o hífen ocupa somente uma posição visual adicional.

## Rollback

Reverter apenas as alterações deste registro nos arquivos listados, preservando trabalhos anteriores e este histórico. Não há dados a restaurar.

## Pendências

Aceite integrado da Fase 1 com API e PostgreSQL reais permanece separado dos testes simulados da interface.
