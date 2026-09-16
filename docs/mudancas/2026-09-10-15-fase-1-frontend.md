# Interface de acesso e cadastros da Fase 1

## Objetivo e motivo
Tornar os campos reconhecíveis com bordas visíveis, foco e exemplos de preenchimento e completar a interface de cadastro, sessão, conta e administração solicitada pelo usuário.

## Escopo e arquivos afetados
apps/web/src, testes web e contratos DESIGN/UX do projeto. Reutilizar tokens existentes em vez de variáveis indefinidas. Backend tem registro independente 14; integração local 16.

## Impacto
Fluxos de conta deixam de ser apenas demonstração. As telas operacionais de entregas permanecem prévias até suas fases. Senhas e tokens não devem ser persistidos em localStorage.

## Validação
Em execução: build/typecheck/lint, Playwright de formulários desktop/mobile, contrato HTTP e interação no navegador.

## Migrações
Não aplicável ao frontend.

## Rollback
Reverter apenas arquivos desta rodada, preservando o cadastro anterior do usuário. Compatibilizar frontend e backend ao reverter contratos.

## Pendências
Registrar testes executados e limites reais ao concluir; serviços externos não configurados não contam como homologados.
