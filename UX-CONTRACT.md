# UX Contract — Coletas

## Atualização de acesso e análise — 2026-09-17

As descrições de prévia abaixo documentam a fundação histórica, não limitam o acesso/cadastro integrado atual. Fontes atuais: AuthController, SessionsController, RecoveryController, AccountDocumentsController e RegistrationReviewsController. Registro: docs/mudancas/2026-09-17-01-aceite-fase-1.md.

AccountInput/AccountFeedback são controles compartilhados das telas de acesso e análise, compostos por Field/Input/PasswordToggleField Brick 0.2.2. Formulários de cadastro público continuam em Registration. AccountPage oferece documentos privados e veículo; AdminReviews usa fila paginada de 20 registros e confirmação AlertDialog. Estado da seleção administrativa fica apenas na memória por conter contexto cadastral restrito; recarregar exige reautenticação e voltar à fila. Não persistir identificadores de pessoas na URL.

Senha fica mascarada com revelação acessível. Login navega à conta; logout limpa memória mesmo se revogação remota não for confirmada. Documento/veículo alterado exige novo login. Recuperação genérica não confirma existência de conta; redefinição remove token do fragmento após leitura. Data de validade é digitada em AAAA-MM-DD na web, sem calendário; validade documental exibida em UTC, histórico administrativo informa fuso do dispositivo. Escolhas curtas usam RadioGroup. Seletor de arquivo nativo de um arquivo é a exceção permitida pelo guia FileUpload; textos do popup pertencem ao navegador/sistema.

Permissões são verificadas no servidor. Conflito 409 mantém motivo e informa recarregamento; não reaplicar decisão automaticamente. Upload não torna arquivo público. Prévia de entregas continua fictícia e sem despacho. Testes: account.spec.ts, phase1-real.spec.ts e IdentityFlowTests. Aceite físico/SMTP externo pendentes; não inferir esses aceites dos mocks ou bundles.

## Contexto e fontes

Portal de estabelecimentos e prévia web da visão do entregador. Fontes: `README.md`, `docs/PLANO-DO-PROJETO.md` (fluxos e fases) e `docs/DECISOES-ARQUITETURAIS.md` DA-003/004. Revisadas em 2026-09-10. A API atual em `src/Coletas.Api/Program.cs` possui prontidão e metadados, não entregas/cobrança/autorização.

Idioma pt-BR; moeda BRL apenas para exemplos. Não há datas operacionais nesta prévia: fuso de operação deverá vir da configuração/API. Alvo WCAG 2.2 AA, sem alegar certificação apenas por teste automatizado. Contrato visual: `DESIGN.md`, runtime `apps/web/src/theme.css`, tema claro.

## Canonical UI Map

| Capability | Canonical owner | Source of truth | Allowed variants | Verification |
|---|---|---|---|---|
| Form | PreviewRequest + Field/Input Brick; Registration compartilhado nos cadastros | Este contrato e IdentityContracts | prévia; empresa; entregador | tests/interface.spec.ts; tests/registration.spec.ts |
| Select/Listbox | Registration select nativo existente | VehicleType; registro 2026-09-16-02 | tipo de veículo; popup do sistema aceito | tests/registration.spec.ts |
| HTTP de cadastro/sessão | apps/web/src/api/client.ts | AuthController; SessionsController | público sem Bearer; conta autenticada | tests/client.spec.ts; IdentityFlowTests |
| Scrollbar | apps/web/src/index.css | DESIGN.md | sem opt-in | tests/design.spec.ts |
| Dialog | FLOWSTACK Brick Dialog | guia 0.2.2 e este contrato | detalhes/prévia | tests/interface.spec.ts |
| Search | DeliveryList + Field/Input Brick | Este contrato | local sintética | tests/interface.spec.ts |
| Status | ConnectionStatus | prontidão existente | idle/busy/success/error | tests/foundation.spec.ts |

Sem seleção de tabela, calendário ou toast nesta mudança. A prévia continua local; os cadastros públicos enviam à API. O cliente de sessão não representa uma tela de login implementada.

### Refatoração de cadastro/sessão — 2026-09-16

Registro: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md. A API já possui identidade e autorização; a descrição inicial acima representa a fundação de 10/09, não o backend atual. Registration é o único formulário de empresa/entregador; retirar o scaffold AccountForms sem consumidores preserva as máscaras e layout existentes. Não ampliar esta refatoração de transporte para uma migração visual. A prévia mantém Field/Input Brick e não muda de dono.

Cadastro espera confirmação do servidor, limpa campos somente em sucesso e permanece na tela aguardando análise. Erros mantêm os dados apenas em memória; timeout de 30 segundos informa resultado incerto e não repete a gravação. Desmontagem cancela a espera local, sem prometer cancelamento no servidor. Consulta BrasilAPI separada envia apenas CNPJ e nunca credenciais.

Login/refresh usam JSON e tokens somente em memória, conforme SessionService; não há cookie de renovação nem restauração após recarregar. Logout usa Bearer e revoga as sessões do titular, inclusive onboarding. Falha de logout limpa memória local e informa revogação remota não confirmada. Respostas de sessão antiga não podem autenticar outra conta. Ainda não há interface de login integrada nesta entrega.

## Navegação e listas

Destinos reais em `?view=overview`, `?view=deliveries`, `?view=courier`. NavList com links reais: Back, refresh e abrir em outra aba preservam o destino. Título `{Tela} — Coletas`. Destino desconhecido mostra recuperação para a visão geral. A prévia não implementa login ou rota 403: isso depende da Fase 1, sem simular autorização.

Busca `q` e filtro `status` restituídos pela URL somente sobre exemplos públicos sintéticos. Busca local imediata; durante composição IME não consolidar query. Limpar restaura foco no Input. Paginação não é necessária para o conjunto fechado de seis exemplos; informar contagem e exibir todos os matches. Integração futura com lista real exige paginação de servidor. Lista de registros, não tabela comparativa: mesmos dados e ações em telas estreitas.

## Flow ledger

| Operação | Resultado | Falha/cancelamento | Foco | Fonte |
|---|---|---|---|---|
| Ver detalhes | abre exemplo em Dialog | Fechar/Escape retorna à lista preservada | trigger | guia Brick Dialog |
| Prévia de solicitação | valida endereço de exemplo e exibe resumo local | preserva campos e mostra erro textual | primeiro inválido | plano fluxo 1; API ainda ausente |
| Fechar prévia | mantém campos na memória até sair/recarregar | não faz descarte implícito entre aberturas | trigger | esta mudança |
| Verificar conexão | consulta readiness com timeout | mensagem persistente e tentar novamente | botão não move | API /health/ready |
| Consultar oferta | somente leitura de dados sintéticos | aceitar indisponível com motivo | navegação normal | plano fases 2/3 |

## Formulário, dados e overlays

Prévia não é cadastro de entrega. Pedir somente endereços de exemplo; sem destinatário/telefone/documento real. `noValidate`, validação no submit, erros associados, required e foco no primeiro inválido. Nenhum cálculo de tarifa, raio, rota ou elegibilidade no React. Sem escrita de rede, cache persistente ou coleta de GPS. Fechamento mantém campos na memória: não há perda em navegação interna. Aviso de saída/reload somente quando há rascunho e o documento será realmente descarregado.

Dialog Brick é dono de foco inicial, Tab, Shift+Tab, Escape, isolamento do fundo, scroll lock e restauração de foco. Campos permanecem em memória no shell mesmo ao trocar tela; modais não são aninhados. Erros ficam junto dos campos, não em toast. Requisições de conexão são canceladas ao desmontar e limitadas a 10 segundos; nenhuma repetição automática. Sucesso só informa prontidão naquele teste, nunca conectividade contínua ou operação disponível.

## Limites de negócio

Não habilitar pagamento, aceite, agrupamento real, disponibilidade, upload ou despacho. As regras são referenciadas no plano; valores de exemplo não são configurações. Status e tarifas sintéticos não são contratos de domínio. Não declarar CNH/veículo/licença aprovado sem backend.

## Verificação

Lint, build/typecheck, testes Playwright de interação, conexão e acessibilidade; comparação painel/entregas/prévia do entregador; desktop e viewport estreito, reduced-motion e forced-colors. Auditoria estática Premium não substitui navegador. Validações e limitações efetivas constam em `docs/mudancas/2026-09-10-01-interface-operacional.md`. Apps nativos e dispositivos físicos permanecem fora deste aceite web.
