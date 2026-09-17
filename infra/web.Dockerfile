FROM node:24-alpine
WORKDIR /app/web
# Motivo: atualizar bibliotecas do sistema detectadas pelo scanner de containers.
# Mudança: docs/mudancas/2026-09-09-03-validacao-integrada.md
RUN apk upgrade --no-cache
COPY apps/web/package*.json ./
# Motivo: npm só instala no build; remover o gerenciador evita carregar dependências vulneráveis desnecessárias no runtime.
RUN npm ci && npm cache clean --force && npm uninstall --global npm
COPY apps/web/ ./
# Preservar o caminho relativo usado pelo validador comum web/mobile.
# Mudança: docs/mudancas/2026-09-17-01-aceite-fase-1.md
COPY apps/shared/ /app/shared/
EXPOSE 5173
CMD ["node", "node_modules/vite/bin/vite.js", "--host", "0.0.0.0"]
