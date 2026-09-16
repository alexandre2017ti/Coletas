# Análise administrativa versionada

## Motivo

A aprovação antiga alterava diretamente o acesso, sem exigir uma análise atualizada. Separar análise e bloqueio evita decisões sobre dados antigos e mantém o histórico.

## Escopo

Implementar estados de análise, versão concorrente própria, fila administrativa, decisões justificadas e histórico com mensagem pública separada da observação interna. Preservar o status legado de acesso para compatibilidade. Bloqueio não apaga a decisão de análise. Aprovação cadastral não comprova licença, capacitação nem elegibilidade operacional.

## Validação

Em 2026-09-15, `dotnet test --no-restore` aprovou 119 testes (0 falhas), incluindo transições, permissões, versão desatualizada, privacidade das notas, documentos ausentes/vencidos/substituídos e bloqueio de JWT emitido. Os testes novos usam EF InMemory: não comprovam transação e concorrência reais no PostgreSQL. Interface, integração PostgreSQL e fluxo completo continuam pendentes. Trabalho interrompido para atender à solicitação de incorporar Ponytail.

## Impacto e migração

Migration aditiva em identidade. Contas ativas existentes terão análise aprovada para preservar o acesso atual. Novas contas começam pendentes. Decisões antigas sem versão deixam de ser aceitas pelo endpoint legado.

## Rollback

Parar a API, preservar backup do banco e reverter a versão da aplicação junto da migration somente após exportar o novo histórico. O Down remove os novos dados de análise; não executar em produção sem autorização.

## Pendências

- [ ] Núcleo e endpoints com testes.
- [ ] Interface administrativa e acompanhamento do cadastro.
- [ ] Homologação PostgreSQL e navegador.
- [ ] Configuração privada do primeiro administrador pelo responsável.
