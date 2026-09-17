# Integração de acesso, documentos e administração

## Motivo
Fechar fluxos reais da Fase 1 após a refatoração, sem confundir serviços implementados com telas integradas.

## Escopo e simplicidade
Reutilizar SessionService, ProfileService, CourierDocumentService e RegistrationReviewService. Expor somente endpoints necessários e compor telas com Brick 0.2.2, cliente HTTP e navegação existentes. Sem nova biblioteca ou arquitetura genérica. Preservar a prévia de entregas e os cadastros existentes.

## Segurança e comportamento
Autorização no servidor para titular/administrador; documentos como attachment privado; decisões versionadas e confirmadas; tokens somente em memória. Recuperação não revela existência de conta. Não simular SMTP configurado. Reiniciar demonstração local é autorizado; produção e lojas não estão no escopo.

## Validação e testes
Consolidada em 17/09: 150 testes .NET aprovados, 90,28% de linhas; 72 testes web aprovados (dois casos opt-in não executados na suíte simulada); build/lint aprovados. Aceite opt-in com navegador e PostgreSQL aprovado: cadastro, onboarding, arquivos privados, decisões, aprovação, login ativo, correção, reprovação e bloqueio concorrente 200/409. A falha real de concorrência foi corrigida no registro 2026-09-17-02. Não inclui SMTP externo nem dispositivos físicos.

## Impacto e migração
Conferir migrations existentes antes de atualizar a demonstração. Não excluir cadastros ou volumes. Documentar bloqueios de SMTP/dispositivo físico.

## Rollback
Reverter apenas esta integração e restaurar a imagem local anterior se necessário; preservar banco, arquivos privados e alterações anteriores. Não executar rollback destrutivo de migration.
