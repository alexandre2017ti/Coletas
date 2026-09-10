# Dependências do aplicativo

O override de uuid para 11.1.1 sob xcode corrige GHSA-w5hq-g745-h8pq na árvore de ferramentas do Expo 57. O consumidor usa uuid.v4(), API disponível nessa versão CommonJS. Motivo e validação: docs/mudancas/2026-09-09-02-fase-zero.md na raiz.

Reavaliar o override ao atualizar Expo/xcode. Validar geração de UUID, expo install --check e exportações Android/iOS. Não executar npm audit fix --force, pois a sugestão atual regride o Expo para SDK 46.
