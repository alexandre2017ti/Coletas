# Atualizar patch do Expo exigido pelo CI

## Objetivo e motivo

O job mobile do workflow nº 8 (execução 35727541619, commit 8a8e02d) falhou em `npx expo install --check` porque o Expo publicou `expo@57.0.24` e o projeto mantinha `57.0.23` instalado. O fail-fast da matriz cancelou `clients (web)`; os demais jobs passaram. É a mesma classe de falha tratada em [2026-09-17-03](2026-09-17-03-patches-expo-ci.md).

## Escopo e escolha Ponytail

- Atualizar somente `expo` para `~57.0.24` e o lockfile; transitivos alterados: `@expo/cli` 57.0.26, `expo-asset` 57.0.18 e `expo-constants` 57.0.19.
- Nenhuma dependência nova, mudança de SDK, código do aplicativo ou configuração do workflow.

## Impacto e segurança

Sem migration de banco. A instalação continua determinística pelo lockfile. Os bundles precisam ser regerados; isso não equivale a publicar nas lojas.

## Validação

Em 22/09/2026, no mobile: `npm test` aprovou 3 testes; `npx tsc --noEmit` aprovou; `npx expo install --check` retornou `Dependencies are up to date`; bundles Android e iOS exportados (1,5 MB cada). Workflow GitHub acompanhado após o push.

## Rollback

Reverter `package.json` e `package-lock.json` juntos, reinstalar com `npm ci` e regerar os bundles. A versão anterior volta a falhar no check online.
