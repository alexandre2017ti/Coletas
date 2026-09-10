# Organização da API para manutenção manual

## Objetivo e motivo
Separar o Program.cs em arquivos compreensíveis para o usuário estudar e manter.

## Escopo e arquivos afetados
Coletas.Api: Program.cs, Controllers (Auth, Couriers, AdminUsers, Tariffs, Platform), Configuration (AuthenticationConfiguration, RateLimitingConfiguration), Responses/IdentityHttpResultMapper e Database/DatabaseMigrationRunner. Backlog e decisões atualizados. Arquivos vazios criados pelo usuário foram preenchidos; alterações anteriores de testes preservadas.

## Impacto
Migração de Minimal APIs para controllers com rotas absolutas e nomes preservados. Serviços, banco, JWT, políticas e rate limit permanecem iguais. MVC passa a responder 400 para enum textual inválido antes do serviço; o formato da validação automática é ValidationProblemDetails e pode diferir do tratamento anterior. Nenhuma funcionalidade nova de negócio.

## Validação
Em execução: build, suíte HTTP e cobertura. Verificar enum inválido, permissões, contratos e ausência de rotas duplicadas. Não implica homologação em PostgreSQL/Ubuntu.

## Migrações
Sem alteração de schema. Comando --migrate preservado e extraído; não executar banco remoto nesta tarefa.

## Rollback
Restaurar o Program.cs anterior e remover apenas os conteúdos introduzidos nos nove arquivos desta extração. Não reverter alterações anteriores de testes ou do usuário. Nenhum rollback de banco necessário.

## Pendências
Explicação guiada arquivo por arquivo; falhas anteriores e novas serão registradas após testes. Sem publicação ou commit nesta tarefa.
