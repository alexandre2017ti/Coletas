# Execução local dos cadastros

## Motivo e escopo
Executar os cadastros solicitados. Corrigir URL padrão para usar o proxy existente do Vite e preservar referência ao formulário após await, evitando erro após cadastro bem-sucedido.

## Arquivos e impacto
Registration.tsx: requisições na mesma origem, encaminhadas para a API. Sem alteração de regras de negócio. Ambiente Compose local isolado coletas-preview com credenciais geradas apenas no processo de inicialização, sem impressão ou versionamento.

## Validação
Em execução: build web e prontidão da API. O ambiente de demonstração não equivale a produção.

## Migrações
Aplicar migrations existentes apenas no banco local de demonstração.

## Rollback
Reverter as duas correções do formulário; parar coletas-preview preservando seu volume. Não apagar dados de outros ambientes.
