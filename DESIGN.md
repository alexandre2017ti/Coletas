---
version: alpha
name: Coletas
description: Central de expedição urbana com leitura rápida de trajetos e próximos passos.
colors:
  primary: "#155f49"
  ink: "#153731"
  canvas: "#f3f6f2"
  surface: "#ffffff"
  muted: "#52645a"
  border: "#d5dfd6"
typography:
  sans:
    fontFamily: "Manrope Variable, Segoe UI, sans-serif"
  mono:
    fontFamily: "ui-monospace, Consolas, monospace"
rounded:
  control: "0.625rem"
  panel: "1rem"
spacing:
  unit: "0.25rem"
  page-max: "90rem"
components:
  button: {}
  card: {}
  dialog: {}
---

# Coletas — identidade de produto

## Overview

Referência: mesa de expedição de um comércio de bairro, com etiquetas de encomendas, trajetos numerados e visão clara do próximo passo. Registro de produto, não landing page. O público usa o portal no balcão e consulta ofertas no celular, conforme `docs/PLANO-DO-PROJETO.md`.

Assinatura: linha de percurso pontilhada, marcadores de coleta e destino e mapa esquemático discreto. Não imitar um mapa real nem um rastreador conectado. A interface demonstrativa deve dizer isso permanentemente.

Preservamos os verdes da fundação. Não usar gradientes de marketing, gráficos financeiros inventados, animações contínuas ou excesso de cartões de indicadores. O idioma é português brasileiro; moeda ilustrativa em BRL. Mercado/região de lançamento ainda dependem da definição operacional do projeto.

Modelo B: `apps/web/src/theme.css` é o dono dos tokens locais; este arquivo espelha os valores aceitos. Os tokens locais alimentam os tokens semânticos públicos de Brick; componentes nunca repetem hexadecimais. Não há pacote Theme compilado. Revisar ambos no mesmo registro de mudança. `apps/web/tests/design.spec.ts` confere a correspondência em runtime.

## Colors

Primary é ação segura e percurso selecionado; ink é texto principal; canvas é a mesa de trabalho; surface são planos de conteúdo; muted é texto secundário; border separa regiões. Os estados success/warning/danger mantêm as famílias semânticas nativas do Brick e sempre possuem rótulo textual.

Somente tema claro nesta entrega, definido no HTML antes da pintura, inclusive portais. Modo escuro requer uma mudança própria e contraste completo, não inversão parcial. Forced-colors mantém as cores de sistema e foco operável.

## Typography

Manrope variável, distribuída localmente pelo pacote de fontes, para identidade e boa leitura em pt-BR. Segoe UI/sans-serif são fallback. Títulos semibold de 24–32px, conteúdo 16px, metadados 14px. Labels de controle e campos preservam receitas Brick, campos nunca abaixo de 16px. Numerais de taxa e distância usam tabular-nums; IDs usam mono.

## Layout

Container largo de até 90rem. Grade Brick: navegação lateral e conteúdo em desktop; navegação em fluxo no celular. Uma única árvore de conteúdo, sem duplicar ações. Breakpoints nativos Brick `md` e `lg`, com reorganização conforme medida legível. Section governa ritmo macro; Stack/Grid usam fatores de 4px. Documento é o dono do scroll; modais têm corpo rolável próprio. Não prender formulários numa altura de tabela.

## Elevation & Depth

Plano principal branco sobre canvas suave. Bordas discretas; sombra reservada a modal, não a todo bloco de texto. O mapa decorativo não compete com status e ações. Sem blur necessário à leitura.

## Shapes

Controles 10px, painéis 16px, marcadores circulares apenas quando significam pontos de trajeto. Ícones lineares de espessura consistente. Estado nunca é reconhecível só pela forma/cor.

## Components

### Donos e mapeamento

| Contrato | Runtime | Consumidores |
|---|---|---|
| colors.primary | --coletas-primary → --brick-color-accent-solid | Button e rota |
| colors.ink | --coletas-ink → --brick-color-text-primary | títulos e texto |
| colors.canvas | --coletas-canvas → --brick-color-surface-canvas | documento |
| colors.surface | --coletas-surface | regiões e mapa |
| colors.muted | --coletas-muted → --brick-color-text-secondary | suporte |
| colors.border | --coletas-border | limites e desenho |
| typography.sans | --coletas-font → --brick-font-family-body | receitas Brick |
| rounded.control / panel | --coletas-radius-control / panel | tokens semânticos Brick |
| spacing.unit / page-max | --coletas-unit / page-max | grade e Container |

Botões: solid/accent para ação principal, outline/neutral para secundária, ghost/neutral para utilidades. Tamanho lg nas ações frequentes. Hover e pressed definidos pelo Brick; foco visível por anel, disabled explicável em texto, busy sem mudança de geometria. Nenhum botão de cobrança ou aceite real habilitado.

NavList marca destino atual. Entregas são uma lista limitada de exemplos independentes; detalhes via Dialog com título, corpo, fechamento e retorno de foco. Busca com Field/Input e botão Limpar. Não usar seletor nativo, calendário ou grid de seleção neste escopo.

Ícones SVG autorais normalizados por Brick Icon; logo e desenho de rotas são arte de aplicação, não componentes interativos. Sem imagens remotas nem dependência de API de mapas.

Movimento apenas de feedback/entrada de modal, usando os contratos Brick. Reduced-motion remove animação espacial. Nenhum marcador pisca ou se move simulando GPS.

Voz direta: “Ver detalhes”, “Prévia de solicitação”, “Verificar conexão”. Valores de exemplo são formatados por Intl pt-BR. Não apresentar taxa calculada, prazo prometido, saldo ou licença aprovada sem servidor.

Scrollbars globais usam tokens de thumb/track/hover/active, propriedades padrão e fallback WebKit. Nenhum container precisa de classe para receber o tema.

## Do's and Don'ts

- Manter taxa, percurso e status próximos; usar a mesma apresentação no painel e detalhes.
- Distinguir “exemplo”, “rascunho nesta página” e “confirmado pelo servidor”.
- Não gravar endereço ou telefone em URL, logs ou armazenamento local.
- Não converter os exemplos de tela em regras de tarifa, raio ou elegibilidade.
