# Interface operacional web

## Objetivo e motivo

Substituir a tela de preparação por uma interface intuitiva, consistente e responsiva, conforme README e solicitação de usar FLOWSTACK UI e Frontend Design Premium. A fundação possui apenas consulta de prontidão: a interface não pode apresentar dados ilustrativos como operação real.

## Escopo e arquivos afetados

- `apps/web`: painel, consulta de entregas demonstrativas, detalhes, prévia de solicitação e experiência ilustrativa do entregador; componentes FLOWSTACK Brick 0.2.2.
- `DESIGN.md`, `UX-CONTRACT.md`, `premium-ui.json`: identidade, responsabilidades e contratos verificáveis.
- Testes web, dependências, documentação de desenvolvimento, README e backlog.
- Sem alteração de API, banco, permissões, tarifa, raio, licença ou capacitação. O aplicativo nativo React Native não recebe componentes DOM; sua implementação visual permanece pendente.

## Decisões e impacto

Dados sintéticos ficam identificados como demonstração. Nenhuma simulação despacha, cobra, calcula rota real ou captura localização. A conexão continua consultando `/health/ready`, sem transformar prontidão em autorização para operar. Prévia de solicitação é mantida apenas na memória da página, sem armazenamento persistente ou envio de dados pessoais. Regras operacionais continuam pertencendo à API.

FLOWSTACK: documentação exata resolvida em instalação temporária de `@flowstack-ui/brick@0.2.2`; manifest e coverage com zero falhas. Guias `layer-selection` e `interface-composition` orientam componentes, layout, CSS e adaptações. Não há importação direta de Atom ou uso de Blocks pagos. A aplicação usa adaptação CSS dos tokens semânticos públicos de Brick, preservando a identidade verde existente; não cria um pacote Colors/Theme ou promete temas compilados.

## Validação e testes

Concluída em 2026-09-10. `npm run lint`, `npm run build`, `npm run test:e2e` (34/34), `npm audit --audit-level=high` (0 vulnerabilidades) e auditoria Axe em todas as telas/estados passaram. Foram capturadas telas desktop e mobile de overview, entregas e entregador. A auditoria Premium strict foi executada, mas seu analisador estático não reconhece os componentes `Button` compostos do Brick e reportou quatro falsos positivos de “actionless-button”; os quatro controles têm destino/ação verificável em Playwright. O JSON de evidência fica em `docs/evidencias/interface-premium-audit.json` e esse limite é mantido explícito, não ignorado.

## Migrações

Nenhuma. Não há mudanças no modelo de dados ou serviços Ubuntu.

## Rollback

Restaurar apenas os arquivos web/documentais relacionados a esta mudança a partir de uma cópia ou revisão anterior; restaurar conjuntamente package.json e package-lock.json e executar npm ci. Não usar reset global: o repositório contém trabalho ainda não versionado. Não apagar volumes, bancos ou registros históricos de mudanças. Não houve publicação remota.

## Pendências

- Integração autenticada com API, geocodificação, tarifa, despacho, GPS, pagamentos e validações operacionais nas fases originais.
- Adaptação visual ao React Native e testes em dispositivos Android/iOS reais.
- Homologação de usabilidade pelos estabelecimentos e entregadores.
