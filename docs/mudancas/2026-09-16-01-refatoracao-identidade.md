# Correções 1, 2 e 3 da revisão de identidade

## Motivo e escopo

Invalidar a análise ao alterar veículo ou documentos, preservar bloqueio, remover decisões antigas duplicadas e emitir JWT exclusivamente pelo SessionService. Sem alteração visual nem publicação.

## Simplicidade

Chamadores dos serviços foram pesquisados. Reutilizar RegistrationReviewService e SessionService; não criar dependência, interface ou camada adicional. A invalidação prepara estado, histórico e revogação; o chamador salva tudo junto com a alteração.

## Validação

- `dotnet test --no-restore --collect:"XPlat Code Coverage" --settings coverage.runsettings`: 140 aprovados, zero falhas (21 casos adicionais).
- Cobertura de linhas: 80,60%; mínimo de 80% aprovado por `scripts/check-coverage.ps1`. Não equivale a 90% ou a cobertura de todos os erros.
- Verificação de formatação dos seis arquivos C# alterados aprovada.
- Regressões: quatro caminhos de invalidação com/sem bloqueio, preservação do motivo de bloqueio, histórico, revogação, perfil atualizado, dados inválidos/sem permissão sem efeitos, veículo inalterado, decisão com versão antiga em dois contextos EF, JWT com sid/scope e sessão persistida. Teste HTTP existente confirma que o token anterior deixa de acessar após documento novo.
- Concorrência exercitada com EF InMemory; não é homologação de transação/rollback no PostgreSQL. Nenhum serviço de demonstração foi atualizado nesta rodada.

## Arquivos afetados

IdentityService, IIdentityService (IdentityContracts), ProfileService, RegistrationReviewService, IdentityFlowTests e RegistrationReviewTests; plano e este registro. Métodos antigos removidos, sem novas dependências.

## Pendências fora deste aceite

Homologação PostgreSQL do fluxo administrativo completo, interface administrativa e atualização da demonstração permanecem pendentes. Esta entrega encerra somente os itens 1, 2 e 3 da revisão.

## Impacto e migração

Não altera o modelo persistido; usa ReviewVersion/ReviewEvents da migration administrativa já existente. Sessões são revogadas quando os dados examinados mudam. Endpoints existentes permanecem.

## Rollback

Reverter somente esta refatoração após preservar mudanças anteriores. Não restaurar versões antigas de aprovação em produção sem avaliação, pois reintroduzem os defeitos corrigidos. Nenhuma exclusão de dados.
