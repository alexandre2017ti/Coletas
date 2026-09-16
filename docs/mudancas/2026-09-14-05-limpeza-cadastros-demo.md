# Limpeza dos cadastros de demonstração

## Objetivo e motivo

Usuário autorizou remover todos os cadastros de teste após a detecção de contatos duplicados, para permitir ativar a exclusividade.

## Escopo

Somente banco `coletas` no container local `coletas-preview-postgres-1`: usuários de empresa/entregador e dependentes por chaves estrangeiras. Inventário: 1 empresa, 4 entregadores, 4 veículos, zero documentos. Configurações, migrations, código e contas administrativas fora da exclusão.

## Validação

Executado: API pausada durante exclusão transacional de 5 usuários (1 empresa e 4 entregadores), com exclusão em cascata dos 4 veículos. Contagens finais de usuários, empresas, entregadores, veículos e documentos: zero. Uma configuração preservada. Migration `20260914171921_UniqueRegistrationIdentifiers` aplicada; os três novos índices únicos foram conferidos no PostgreSQL.

API local atualizada com publish Release já validado na rodada anterior. Pelo proxy do site, cadastro sem CPF retornou 400 com validação de CPF e `/health/ready` retornou 200. A validação não criou registros novos.

O build Docker padrão acessou as imagens Microsoft, mas falhou no download de pacotes Ubuntu. Fallback local implementado em `infra/api.cached.Dockerfile` e `scripts/refresh-preview-cached.sh`: empacota `artifacts/registration-api/` sobre o runtime instalado, mantém credenciais em memória e executa migration/Compose. Isso atualizou de fato a demonstração na porta 5080. O fallback não substitui build integral para produção; executar publish Release atualizado antes de reutilizá-lo.

## Impacto e rollback

Cadastros serão apagados conforme autorização, sem backup novo dos dados descartáveis. Sem recuperação automática; será necessário recadastrar. Configurações e histórico de migrations preservados. Reverter documentação não restaura dados.

## Pendências

Cadastros disponíveis para novos testes com CPF obrigatório e exclusividade ativa. Build integral para produção continua dependendo do acesso aos repositórios Ubuntu. Nenhum commit ou push executado nesta rodada.
