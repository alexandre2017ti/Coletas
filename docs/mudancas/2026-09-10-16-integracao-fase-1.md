# Integração e aceite local da Fase 1

## Objetivo e motivo
Concluir os fluxos de acesso/cadastros e corrigir a usabilidade dos formulários apontada pelo usuário. Reunir a evidência de integração sem confundir mocks com operação real.

## Escopo
Integração local em Ubuntu WSL2, persistência privada dos arquivos, roteiro reproduzível de demonstração e atualização do backlog. Backend e frontend têm registros próprios desta rodada.

## Arquivos afetados
compose.yaml, infra/api.Dockerfile, scripts de demonstração, docs/DESENVOLVIMENTO.md e docs/PLANO-DO-PROJETO.md conforme necessário.

## Impacto
Ambiente local mantém os dados existentes. Nenhum envio de e-mail externo, publicação remota, commit ou push faz parte desta validação.

## Validação
Em execução: formulários desktop/mobile, cadastro/login/documentos/permissões, migrations no PostgreSQL e testes automatizados.

## Migrações
Aplicar as novas migrations apenas no banco coletas-preview local após revisão. Não apagar nem recriar volume de dados existente.

## Rollback
Parar apenas os containers do ambiente local. Preservar volumes de banco e documentos. Reverter arquivos da integração em conjunto com os contratos correspondentes; não executar downgrade de banco sem revisar os dados adicionados.

## Pendências
Resultados serão registrados após execução. Credenciais de provedor SMTP, validação externa de documentos e homologação de dispositivos devem ser declaradas separadamente se não disponíveis.
