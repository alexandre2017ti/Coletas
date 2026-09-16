# Fallback exclusivamente local: runtime já instalado com libgssapi, sem downloads.
# Motivo e limitações: docs/mudancas/2026-09-14-05-limpeza-cadastros-demo.md
FROM coletas-local-runtime:cached
USER root
COPY . /app/
RUN mkdir -p /app/private/documents /app/private/mail && chown -R app:app /app/private && chmod -R 700 /app/private
USER app
WORKDIR /app
ENTRYPOINT ["dotnet", "Coletas.Api.dll"]
