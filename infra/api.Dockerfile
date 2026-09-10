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
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Coletas.Api.dll"]
