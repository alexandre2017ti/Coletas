# Executar e validar a fundação

## Pré-requisitos

- SDK .NET 10.0.3xx (global.json aceita patches da mesma faixa).
- Node.js 24 LTS e npm. A validação local inicial também funcionou em Node 26.
- Docker Engine/Desktop com Compose v2 e containers Linux.
- Para aplicativo: Expo; aparelho na mesma rede ou emulador. Compilar e assinar iOS exige macOS/Xcode ou serviço de build remoto. Exportar o bundle não produz APK/IPA.

## Estrutura

- Coletas.slnx: solução .NET.
- src/Coletas.Domain: entidades e invariantes.
- src/Coletas.Application: contratos e casos de uso.
- src/Coletas.Infrastructure: EF Core, PostgreSQL/PostGIS, Redis e verificações.
- src/Coletas.Api: composição e HTTP.
- src/MODULES.md: limites e nomes dos módulos futuros.
- tests/Coletas.Tests: testes HTTP, configuração e persistência.
- apps/web: React/TypeScript/Vite, tela inicial e testes de navegador.
- apps/mobile: React Native/Expo/TypeScript, tela inicial.
- infra/, compose.yaml: ambiente local.
- .github/workflows/ci.yml: verificações; execução remota depende de hospedar o repositório no GitHub.

## Ambiente completo com Docker

### Runtime instalado neste computador

Existe agora o Ubuntu 24.04 WSL2 exclusivo `coletas-dev`, com Docker Engine e Compose. Use `./scripts/docker.ps1` no lugar de `docker` ao trabalhar pelo PowerShell. O registro WSL de outra aplicação foi preservado.

```powershell
./scripts/docker.ps1 version
./scripts/validate-compose.ps1
```

O segundo comando executa um aceite descartável: cria senha em memória, containers e volume com nome exclusivo, compila as imagens, verifica banco/seed/PostGIS/Redis/API/proxy e reexecuta a migration. Ao terminar, remove somente os dados e containers criados por essa execução. Não serve para armazenar dados de desenvolvimento. Portas 5080, 5173, 5432 e 6379 precisam estar livres.

No Ubuntu ou no runner CI, execute `bash scripts/validate-compose.sh`. Para preparar um Ubuntu 24.04 isolado novo, o script versionado `scripts/prepare-linux-runtime.sh` instala os pacotes do Docker dos repositórios Ubuntu. Não o execute em servidor de produção.

Registro e evidências: [2026-09-09-03-validacao-integrada.md](mudancas/2026-09-09-03-validacao-integrada.md).

Na raiz, copie .env.example para .env e defina POSTGRES_PASSWORD e JWT_SIGNING_KEY com valores aleatórios locais. A chave JWT deve ter no mínimo 32 caracteres. Use somente letras/números para não precisar escapar a connection string. Não compartilhe nem versione o arquivo.

```powershell
Copy-Item .env.example .env
```

Edite .env localmente antes dos próximos comandos; não sobrescreva um .env já configurado.

```powershell
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

O serviço migrate aplica a migration antes da API. A execução normal da API não altera o banco. Os dados iniciais contêm apenas DeliveryGrouping.RadiusMeters=5000; não há contas ou senhas de demonstração.

- Site: http://localhost:5173
- API: http://localhost:5080/api/v1/platform
- Cotação tarifária: `POST http://localhost:5080/api/v1/tariffs/quote` (prévia, não confirma entrega)
- Cadastro: `POST /api/v1/auth/register/establishments` ou `POST /api/v1/auth/register/couriers`
- Login: `POST /api/v1/auth/login`; exige `Jwt__SigningKey` somente no ambiente, com no mínimo 32 caracteres
- Processo ativo: http://localhost:5080/health/live
- Banco/esquema/PostGIS/cache prontos: http://localhost:5080/health/ready
- OpenAPI JSON (Development): http://localhost:5080/openapi/v1.json

O botão de conexão do site verifica readiness, não apenas processo ativo.

```powershell
Invoke-RestMethod http://localhost:5080/api/v1/platform
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/v1/tariffs/quote -ContentType 'application/json' -Body '{"routeDistanceKm":7,"requiresOperationalReturn":true}'
Invoke-WebRequest http://localhost:5080/health/ready
docker compose down
```

Os cadastros da Fase 1 começam com status `Pending`: login de onboarding permite acompanhar análise e enviar documentos, sem liberar operação. Contas aprovadas usam login normal e renovação por token de uso único. Upload privado e recuperação estão implementados; entrega de e-mail depende de SMTP configurado. Nunca coloque credenciais em arquivos versionados. Consulte [homologação da Fase 1](HOMOLOGACAO-FASE-1.md).

Parar com down preserva o volume. Não use down -v para parar: essa opção apaga os dados. Redis é temporário na fundação. As portas estão vinculadas a 127.0.0.1 e o Compose é de desenvolvimento; não é uma configuração de produção.

## API e site fora do Docker

Inicie PostgreSQL/PostGIS e Redis pelo Compose. Configure Infrastructure__Postgres e Infrastructure__Redis no ambiente local da API, sem inserir valores reais nos arquivos versionados. Alternativa: dotnet user-secrets após configurar UserSecretsId. Os exemplos .env não são carregados automaticamente pelo .NET; Compose injeta as variáveis.

```powershell
dotnet tool restore
dotnet restore --locked-mode
dotnet run --project src/Coletas.Api -- --migrate
dotnet run --project src/Coletas.Api --launch-profile http
```

Em outro terminal:

```powershell
cd apps/web
npm ci
npm run dev
```

O proxy do Vite encaminha /api e /health para localhost:5080. API_PROXY_TARGET pode alterar esse destino. Clientes web nunca recebem credenciais do banco.

## Aplicativo Android/iOS

```powershell
cd apps/mobile
npm ci
npm run start
```

Para testar conexão, copie apps/mobile/.env.example para .env e defina EXPO_PUBLIC_API_URL. Android Emulator usa normalmente http://10.0.2.2:5080; aparelho físico precisa do IP LAN do computador. A API deve estar acessível nessa interface: em desenvolvimento local use --urls http://0.0.0.0:5080 em uma rede confiável, com a regra de firewall necessária. O Compose padrão não expõe portas à LAN. Variáveis EXPO_PUBLIC_* são incorporadas ao aplicativo e não podem conter segredos.

Nesta fase o aplicativo oferece cadastro com CPF, login, situação cadastral e envio de documentos. Não solicita GPS nem recebe chamadas reais. A homologação desses fluxos em aparelhos físicos é pendência da Fase 1; push e localização são fases posteriores.

## Validação

Na raiz:

```powershell
node scripts/check-documentation.mjs
dotnet restore --locked-mode
dotnet build -c Release --no-restore -warnaserror
dotnet format --verify-no-changes --no-restore
dotnet test -c Release --no-build --settings coverage.runsettings --collect:"XPlat Code Coverage"
./scripts/check-coverage.ps1
dotnet ef migrations has-pending-model-changes --project src/Coletas.Infrastructure --startup-project src/Coletas.Api
```

Em apps/web: npm run lint, npm run build, npx playwright install chromium, npx playwright test.
Em apps/mobile: npm run typecheck, npx expo install --check, npm run export:android, npm run export:ios.

Cobertura mínima: 80% das linhas próprias. Migrations, snapshots e fontes geradas pelo OpenAPI não compõem essa métrica; validar migrations por revisão do SQL e integração com banco real.

## Migrations e rollback

```powershell
dotnet ef migrations add NomeDaMudanca --project src/Coletas.Infrastructure --startup-project src/Coletas.Api --output-dir Persistence/Migrations
dotnet format
dotnet ef migrations script --idempotent --project src/Coletas.Infrastructure --startup-project src/Coletas.Api --output artifacts/migrations.sql
```

Revisar o SQL antes de aplicar. A primeira migration cria configuration.SystemSettings e habilita PostGIS. Down remove a tabela e preserva extensão/schema; use somente em banco descartável ou com plano de backup/restauração. Não usar EnsureCreated.

## Qualidade e rastreabilidade

Cada alteração exige registro novo em docs/mudancas/. O verificador exige inclusão de um registro com as seções principais; a revisão humana verifica motivo, impacto, comentários no código e pendências. Para impedir merge em GitHub, configurar os jobs deste workflow como checks obrigatórios na proteção da branch. A proteção não foi configurada remotamente nesta fase.

As versões NuGet e lockfiles foram fixados. Atualizações de dependências também exigem registro e nova validação. A justificativa do override transitivo mobile está em apps/mobile/DEPENDENCIES.md.

## Referências técnicas consultadas

- [Npgsql EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html)
- [Templates Expo](https://docs.expo.dev/more/create-expo/)

## Limites de validação desta entrega

Fase 0 validada em containers Linux no Ubuntu WSL2 local: build, migration inicial/reexecução, seed, PostGIS, Redis, proxy web, restart e readiness 200. A parada do Redis produziu readiness 503 sem derrubar liveness; após recuperação, readiness voltou a 200.

Scanning das imagens próprias passou para HIGH/CRITICAL com correção disponível. A imagem web inicia Vite diretamente pelo Node; npm só é usado durante o build e não está presente no container em execução.

Build/testes locais e exportação de bundles são distintos de publicação. Workflow remoto e proteção da branch dependem da definição do repositório remoto. Não houve implantação em Ubuntu remoto ou publicação nas lojas. O roteiro descarta somente seus próprios dados de teste; dados persistentes de desenvolvimento usam o Compose padrão.
