# UX Contract — Coletas

## Contexto e fontes

Portal de estabelecimentos e prévia web da visão do entregador. Fontes: `README.md`, `docs/PLANO-DO-PROJETO.md` (fluxos e fases) e `docs/DECISOES-ARQUITETURAIS.md` DA-003/004. Revisadas em 2026-09-10. A API atual em `src/Coletas.Api/Program.cs` possui prontidão e metadados, não entregas/cobrança/autorização.

Idioma pt-BR; moeda BRL apenas para exemplos. Não há datas operacionais nesta prévia: fuso de operação deverá vir da configuração/API. Alvo WCAG 2.2 AA, sem alegar certificação apenas por teste automatizado. Contrato visual: `DESIGN.md`, runtime `apps/web/src/theme.css`, tema claro.

## Canonical UI Map

| Capability | Canonical owner | Source of truth | Allowed variants | Verification |
|---|---|---|---|---|
| Form | PreviewRequest + Field/Input Brick | Este contrato | prévia sem envio | tests/interface.spec.ts |
| Scrollbar | apps/web/src/index.css | DESIGN.md | sem opt-in | tests/design.spec.ts |
| Dialog | FLOWSTACK Brick Dialog | guia 0.2.2 e este contrato | detalhes/prévia | tests/interface.spec.ts |
| Search | DeliveryList + Field/Input Brick | Este contrato | local sintética | tests/interface.spec.ts |
| Status | ConnectionStatus | prontidão existente | idle/busy/success/error | tests/foundation.spec.ts |

Sem seleção de tabela, calendário, select/listbox, toast, CRUD remoto ou permissões nesta mudança. Não criar donos fictícios para capacidades não implementadas.

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
