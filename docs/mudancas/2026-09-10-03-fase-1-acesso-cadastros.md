# Mudança 2026-09-10-03 — Fase 1: acesso e cadastros

## Objetivo

Implementar o primeiro recorte executável da Fase 1: cadastro de contas de estabelecimento e entregador, autenticação por token de curta duração, autorização por perfil, veículo, registro de documentos e fluxo administrativo de aprovação ou bloqueio.

## Motivo

A operação não pode receber entregas sem distinguir os perfis e sem validar que o entregador foi aprovado. A fundação anterior possuía apenas verificações técnicas e configurações; esta mudança cria a base de identidade e cadastro necessária para as fases de entrega e distribuição.

## Escopo

- Criar usuários com perfil `Establishment`, `Courier`, `Operator` ou `Admin`.
- Armazenar senha somente como hash BCrypt; nunca persistir senha em texto.
- Normalizar e validar e-mail e senha no cadastro.
- Emitir JWT de curta duração após login de conta ativa.
- Aplicar limitação de tentativas aos endpoints de autenticação.
- Cadastrar estabelecimento.
- Cadastrar entregador com telefone WhatsApp e veículo.
- Registrar metadados de documentos do entregador, incluindo CNH e documento do veículo.
- Permitir aprovação e bloqueio por usuário com perfil administrativo.
- Manter o cadastro pendente até aprovação; não liberar usuário pendente para login.

## Fora do escopo

- Recuperação de senha por e-mail/WhatsApp.
- Refresh token e revogação distribuída de sessão.
- Upload binário e armazenamento em R2/S3.
- Validação automática de CNH, placa, multas ou pendências externas.
- Integração de pagamento, licença semanal ou palestras.
- Interface web/mobile conectada aos endpoints.
- Criação de conta administrativa inicial.

## Arquivos afetados

- `docs/PLANO-DO-PROJETO.md`
- `docs/DESENVOLVIMENTO.md`
- `docs/mudancas/2026-09-10-03-fase-1-acesso-cadastros.md`
- `src/Coletas.Domain/Identity/*`
- `src/Coletas.Domain/Establishments/*`
- `src/Coletas.Domain/Couriers/*`
- `src/Coletas.Domain/Documents/*`
- `src/Coletas.Application/Identity/*`
- `src/Coletas.Application/Registrations/*`
- `src/Coletas.Infrastructure/Identity/*`
- `src/Coletas.Infrastructure/Registrations/*`
- `src/Coletas.Infrastructure/Persistence/*`
- `src/Coletas.Infrastructure/Persistence/Migrations/20260910164207_Phase1AccessAndRegistrations.cs`
- `src/Coletas.Infrastructure/Persistence/Migrations/20260910164207_Phase1AccessAndRegistrations.Designer.cs`
- `src/Coletas.Infrastructure/Persistence/Migrations/ColetasDbContextModelSnapshot.cs`
- `src/Coletas.Api/Program.cs`
- `src/Coletas.Api/appsettings.json`
- `compose.yaml`
- `.env.example`
- `tests/Coletas.Tests/*`

## Impacto

- A API passa a exigir uma chave JWT fornecida por ambiente para iniciar.
- Usuários e documentos passam a ser dados sensíveis, sem respostas contendo hash de senha ou arquivo privado.
- Contas novas ficam pendentes até aprovação administrativa.
- O estado de aprovação é armazenado no banco e não em um booleano isolado de licença.
- A migration cria tabelas e schemas novos; deve ser revisada antes de aplicação em ambiente persistente.

## Segurança

- BCrypt com fator configurável pelo código da biblioteca será usado para senha.
- JWT usa chave, emissor e audiência fornecidos por configuração/segredo do ambiente.
- O login retorna mensagem genérica para não revelar se o e-mail existe.
- Endpoints de login e cadastro terão rate limit.
- Aprovação, bloqueio e documentos exigem autenticação e autorização.
- Arquivos binários não serão aceitos nesta mudança; isso evita armazenamento privado incompleto.

## Validação

- `dotnet restore Coletas.slnx`: aprovado.
- `dotnet build Coletas.slnx --no-restore`: aprovado, 0 avisos e 0 erros.
- `dotnet test Coletas.slnx --no-restore`: aprovado, 15 testes.
- `dotnet ef migrations has-pending-model-changes --project src/Coletas.Infrastructure --startup-project src/Coletas.Api`: aprovado, sem alterações pendentes.
- Migration `20260910164207_Phase1AccessAndRegistrations` revisada; cria apenas os schemas/tabelas do escopo.
- Testes HTTP confirmam rejeição de rotas protegidas sem autenticação.
- `dotnet format Coletas.slnx --verify-no-changes --no-restore`: aprovado.
- `scripts/check-documentation.mjs` e `git diff --check`: aprovados.
- Docker com banco real, login com conta persistida, autorização com perfis reais, upload e rate limit ainda não foram validados nesta etapa; o Docker CLI não está disponível neste shell.

## Migração

Migration EF Core versionada criada para identidade, estabelecimentos, entregadores, veículos e documentos. Não usar `EnsureCreated`. O SQL foi revisado estruturalmente; a aplicação em banco real continua pendente por indisponibilidade do Docker neste shell.

## Rollback

Remover a migration somente antes de aplicá-la em ambiente persistente. Após aplicação, usar a migration `Down` apenas em banco descartável ou restaurar backup; desativar endpoints por configuração/versão sem apagar dados históricos.

## Pendências

- Recuperação de senha e refresh token seguro.
- Upload privado com limite de tamanho, allowlist de MIME, antivírus e R2/S3.
- Verificação externa de CNH, veículo e pendências.
- Interface web/mobile para cadastro, login e acompanhamento da aprovação.
- Seed administrativo seguro via procedimento operacional, sem credencial versionada.
- Auditoria detalhada de login, aprovação, bloqueio e alteração de documentos.
