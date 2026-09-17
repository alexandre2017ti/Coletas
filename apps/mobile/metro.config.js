const path = require('node:path');
const { getDefaultConfig } = require('expo/metro-config');
const config = getDefaultConfig(__dirname);
// Somente validadores puros compartilhados; não trazer dependências React da web.
// Mudança: docs/mudancas/2026-09-16-04-mobile-acesso-cpf.md
config.watchFolders = [...config.watchFolders, path.resolve(__dirname, '../shared')];
module.exports = config;
