# Revalidação da fundação após a Fase 1

## Objetivo e motivo

Restabelecer o aceite Ubuntu/Docker após a inclusão da chave JWT obrigatória e da segunda migration. Docker está acessível pelo wrapper scripts/docker.ps1 no Ubuntu WSL2 coletas-dev.

## Escopo e arquivos afetados

scripts/validate-compose.sh, docs/PLANO-DO-PROJETO.md e este registro. Gerar segredo efêmero e comparar as migrations aplicadas com as migrations versionadas, incluindo reexecução idempotente. Não alterar dados persistentes de desenvolvimento.

## Impacto e migrações

O aceite aplica as migrations existentes somente no banco descartável criado pela execução. Nenhuma migration nova.

## Validação

- Aceite integrado aprovado (exit 0) via scripts/validate-compose.ps1 no Ubuntu coletas-dev: build Linux, aplicação das duas migrations, reexecução sem duplicação, seed, consultas PostGIS, Redis PONG, API e proxy web, reinício da API e recuperação da prontidão após queda do Redis.
- Containers, rede e volume descartáveis da execução foram removidos automaticamente. Nenhum dado persistente do usuário foi removido.
- dotnet test com coverage.runsettings: 15/15 aprovados; cobertura de linhas 55,16%. O check de 80% falhou e permanece obrigatório. O aceite da infraestrutura não equivale ao aceite do CI completo após a Fase 1.
- Verificador documental aprovado. Git sem commits e sem remoto configurado.

## Rollback

Reverter as mudanças do script. A limpeza do aceite remove somente containers e volumes do projeto exclusivo criado nessa execução.

## Pendências

Execução remota do CI e proteção de branch exigem URL do repositório e acesso ao destino. Não declarar essas pendências concluídas com base em testes locais.

Completar testes de identidade/cadastros da Fase 1 para recuperar cobertura mínima antes de aprovar o CI. A aplicação das migrations foi validada em banco descartável, não em desenvolvimento persistente ou produção.
