FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN dotnet restore src/Coletas.Api --locked-mode
RUN dotnet publish src/Coletas.Api -c Release --no-restore -o /out /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# Motivo: Npgsql tenta carregar GSSAPI em Linux; a imagem base não inclui a biblioteca.
# Mudança: docs/mudancas/2026-09-09-03-validacao-integrada.md
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /out .
# Motivo: documentos e mensagens locais ficam fora da raiz pública, graváveis
# pelo usuário sem privilégios e persistidos no volume. Mudança: 2026-09-10-16-integracao-fase-1.md.
RUN mkdir -p /app/private/documents /app/private/mail && chown -R "$APP_UID" /app/private && chmod -R 700 /app/private
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Coletas.Api.dll"]
