# Workflow verde após o envio das mudanças locais

## Objetivo e motivo

Registrar o resultado do acompanhamento do workflow GitHub iniciado nesta rodada: os commits pendentes foram enviados, o workflow nº 8 falhou no job mobile por patch do Expo, a correção foi validada e o workflow nº 9 concluiu com sucesso.

## Escopo e escolha Ponytail

Somente documentação e status do backlog; sem alteração de código. O ajuste de dependência está registrado em [2026-09-22-01](2026-09-22-01-atualizar-patch-expo.md).

## Validação

- Workflow nº 8 (execução 35727541619, commit 8a8e02d): backend, compose e image-scan aprovados; `clients (mobile)` falhou no `expo install --check` e `clients (web)` foi cancelado pelo fail-fast da matriz.
- Workflow nº 9 (execução 35728533517, commit cae638f): seis jobs aprovados — backend, clients (web), clients (mobile), compose, image-scan (api) e image-scan (web).
- O log do job mobile foi conferido com a credencial local do Git; nenhum segredo foi exibido ou versionado.

## Rollback

Não há artefato a reverter. Para desfazer, editar novamente o backlog; o histórico dos workflows permanece no GitHub.

## Pendências

A proteção da branch continua inacessível ao conector (403); a configuração manual pelo proprietário segue pendente.
