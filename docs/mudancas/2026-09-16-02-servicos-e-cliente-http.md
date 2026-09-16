# Itens 4 e 5 — serviços de cadastro e cliente HTTP

## Motivo e escopo

Separar documentos e bootstrap administrativo do ProfileService, preservar a invalidação centralizada e unificar a comunicação dos cadastros web. Retirar o formulário genérico sem consumidores, mantendo o Registration compartilhado por empresa/entregador e suas máscaras. Alinhar o cliente de sessão com endpoints reais de renovação/saída, sem acrescentar tela de login nesta rodada.

## Simplicidade

Reutilizar SessionService, RegistrationReviewService, validadores e formulário existentes. Separação por responsabilidade concreta, sem novas interfaces ou dependências. Transporte de cadastro compartilhado com autenticação, com timeout e sem repetição automática de gravações incertas. Consulta externa CNPJ permanece separada e nunca recebe credenciais.

## Contrato de sessão

Access e refresh tokens ficam apenas em memória. Login/renovação entregam tokens no JSON; refresh recebe RefreshRequest. Recarregar a página exige novo login. Não prometer cookie HttpOnly inexistente. Logout autenticado revoga sessões do titular; limpar memória local mesmo em erro, informando falha de confirmação remota. Nenhum token em URL, logs ou armazenamento persistente.

## Validação

- dotnet test --no-restore --collect:"XPlat Code Coverage" --settings coverage.runsettings: 145 aprovados, zero falhas; relatório a3fa56fa-aaf1-479a-9c2b-9255ef9c40e4.
- scripts/check-coverage.ps1: 84,03% de linhas, acima do mínimo de 80%. Cobertura não significa percentual de erros possíveis detectados.
- dotnet format --no-restore --verify-no-changes nos arquivos desta refatoração: aprovado.
- npm --prefix apps/web run lint e build: aprovados.
- PLAYWRIGHT_PORT=5191 npm --prefix apps/web run test:e2e: 66 aprovados em desktop e mobile emulados. API externa simulada no navegador; testes HTTP .NET usam EF InMemory.
- Cenários novos: rotação/reuso de refresh, logout autenticado e onboarding, refresh inválido, contrato textual de perfil, cache no-store, renovação concorrente no cliente, limpeza local em falha de logout, erro 400/409/503/rede com campos preservados e timeout real de 30 segundos sem POST duplicado.
- Primeiro ciclo web: 63/64, falha na comparação de largura do botão de conexão com carregamento de fonte. O teste agora aguarda document.fonts.ready antes de medir; a asserção de largura permanece exata. Ciclo completo posterior: 66/66.
- Gate documental e git diff --check: aprovados.

Auditoria Premium strict executada; artefato local artifacts/premium-audit-refactor.json. A ausência de decisão explícita sobre o select existente foi resolvida no contrato/manifesto. Restam quatro falsos positivos de actionless-button: AppErrorBoundary usa Button href; DeliveryList usa Dialog.Close; PreviewRequest usa Dialog.Trigger e Dialog.Close. As ações são fornecidas por composição Brick, não por onClick literal. Inspeção de código e testes dos diálogos confirmam o funcionamento; não declarar auditoria estática totalmente aprovada nem alterar componentes corretos para satisfazer a heurística.

Ponytail orientou reutilização e retirada do scaffold; .NET/React orientaram divisão concreta e cancelamento. Premium orientou preservação do comportamento e registro do contrato; Test Master orientou testes positivos, negativos e estabilização da medição, sem relaxar asserções.

## Organização resultante

- ProfileService: leitura de perfil/listagem e edição de veículo.
- CourierDocumentService: upload, download e decisão documental.
- AdminBootstrapService: primeiro administrador pelo console, sem endpoint público.
- CourierAccess: regra interna compartilhada de titular/administrador.
- Registration: formulário existente compartilhado pelos dois cadastros.
- api/client.ts: transporte da API, erros, timeout e contrato de sessão.
- SessionsController: refresh e logout vinculados ao SessionService existente; removido logout alternativo sem consumidores.

## Impacto e migração

Sem mudança de esquema, regras comerciais, paleta ou layout. Serviços extraídos continuam com as regras de propriedade e bloqueio. A demonstração não é atualizada automaticamente. AccountForms.tsx era scaffolding sem consumidores; sua remoção não retira telas existentes.

## Rollback

Reverter somente esta mudança, preservando as correções dos itens 1–3 e demais alterações do usuário. Código do formulário genérico preservado em artifacts/refactor-backup/AccountForms.tsx (artefato local ignorado pelo Git); sem exclusão de dados de cadastro.

## Limites

Não foram executados homologação PostgreSQL, teste de corrida relacional do bootstrap, publicação, commit/push ou atualização dos containers de demonstração. Nenhuma tela de login foi acrescentada. Melhorias visuais adicionais dos cadastros, incluindo controle de revelar senha e proteção de rascunhos na navegação, não fazem parte desta refatoração de transporte.
