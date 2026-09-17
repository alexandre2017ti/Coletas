# Patches compatíveis do Expo

## Motivo
A verificação online `expo install --check` e o job mobile do workflow 35223386485 identificaram patches obrigatórios para o SDK 57: expo 57.0.23, document-picker 57.0.2, file-system 57.0.7 e secure-store 57.0.4. A verificação offline anterior não comprovava essa compatibilidade.

## Escopo
Atualizar somente esses quatro patches e o package-lock do mobile. Reutilizar a configuração atual, sem nova biblioteca nem mudança de SDK principal.

## Impacto e migração
Sem migration de banco; reinstalar dependências pelo lockfile. Bundles precisam ser regerados. Não equivale a atualização de aplicativo publicado nas lojas.

## Validação
Em 17/09, `npm test` aprovou três testes, `npm run typecheck` aprovou, `npx expo install --check` retornou dependências atualizadas e os bundles Android (596 módulos) e iOS (598 módulos) foram gerados. Um novo workflow GitHub será acompanhado após o push.

## Rollback
Reverter package.json e package-lock juntos, reinstalar com npm ci e regerar bundles. A versão anterior volta a falhar no check online conhecido.
