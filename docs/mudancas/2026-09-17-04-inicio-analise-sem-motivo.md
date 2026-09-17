# Início de análise sem motivo obrigatório

## Motivo

As ações administrativas têm resultado conhecido pelo próprio estado. Exigir um campo público em toda ação incentivava registros artificiais como “teste” no histórico do titular.

## Escopo

- A API registra uma mensagem padrão para cada decisão quando o administrador não escreve uma mensagem: análise iniciada, aprovação, correção, rejeição, bloqueio ou desbloqueio.
- A tela administrativa exibe um botão próprio para iniciar a análise, sem campos de motivo ou observação; nas demais decisões, a mensagem ao titular é opcional.
- Aprovação e reprovação de documentos também aceitam mensagem opcional e registram `Documento aprovado.` ou `Documento reprovado.` quando ela estiver vazia.

## Validação

- Testes de serviço verificam as mensagens neutras de início e aprovação sem texto digitado.
- Teste de aceite PostgreSQL passa a iniciar análises sem preencher motivo e confirma a transição para `Em análise`.

## Rollback

Restaurar a obrigatoriedade de `Reason`, remover as mensagens padrão e reaplicar o formulário único. Isso volta a exigir justificativa para cada decisão.
