# Cadastro integrado com PostgreSQL

## Objetivo e motivo

Validar os cadastros de entregador e estabelecimento pela API real e conferir persistência. O modelo atual contém alterações anteriores da Fase 1 sem migration correspondente, impedindo a atualização segura do ambiente local.

## Escopo

Gerar e revisar migration aditiva das alterações existentes; atualizar o ambiente local Ubuntu/Docker; executar aceite HTTP com dados de teste identificados e confirmar valores no PostgreSQL. Registrar evidências e atualizar o plano conforme o resultado.

## Validação e testes

Executado em 2026-09-14: `dotnet test --no-restore` (71 aprovados), publish Release e `scripts/validate-registration-local.sh` aprovado. Nove respostas conferidas: dois cadastros 201, duas duplicidades 409, três entradas inválidas 400 e dois logins pendentes 401. Consultas ao PostgreSQL confirmaram normalização de telefone/placa/CNPJ, vínculo de veículo e exatamente dois usuários; a limpeza confirmou zero usuários do aceite. Respostas do contrato atual serializam `Pending` como 0. A primeira execução identificou essa divergência na expectativa do teste, corrigida antes do aceite final.

O build Docker normal falhou por falta de rota ao registry Microsoft. O aceite usou publish atual montado somente para leitura no runtime Linux já existente, em container temporário na porta 5081, conectado ao PostgreSQL real da demonstração. O container temporário foi removido ao finalizar. Nenhum aceite de produção ou publicação está incluído.

## Impacto e migrações

Migration `20260914122813_Phase1SessionAndDocumentFields`: seis colunas novas, tabelas IdentityAudits e SecurityTokens e índices. Up revisado sem remoções; aplicado no PostgreSQL local existente. Segunda execução confirmou banco atualizado sem reaplicar. Snapshot atualizado. Scripts novos: `scripts/validate-registration-local.sh` e `scripts/validate-registration-local.py`. Artefatos de publish ficam em `artifacts/` ignorado pelo Git.

## Rollback

Antes de qualquer downgrade, salvar o banco e revisar as tabelas/colunas criadas: sua remoção perde dados de sessões e documentos. Preferir corrigir aditivamente. Dados de teste serão removidos somente pelos identificadores criados nesta execução.

## Pendências

Homologar navegador → proxy → API atualizada → PostgreSQL. O preview 5080 continua com imagem anterior; a imagem Docker atual ainda precisa ser reconstruída quando houver acesso ao registry. O aceite não valida recuperação de senha, aprovação administrativa, upload ou sessões completas da Fase 1.
