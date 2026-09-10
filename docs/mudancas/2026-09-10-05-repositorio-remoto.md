# Repositório remoto do projeto

## Objetivo e motivo

Vincular a cópia local ao repositório informado pelo usuário: https://github.com/alexandre2017ti/Coletas. Isso define o destino para o histórico e o CI.

## Escopo e arquivos afetados

Configuração Git local de origin, docs/PLANO-DO-PROJETO.md e este registro. Nenhum código de aplicação alterado.

## Impacto

O remoto origin aponta para o destino confirmado. A associação não envia arquivos nem executa o workflow.

## Validação

git remote get-url origin confirmou a URL. git ls-remote origin terminou com sucesso sem referências retornadas. A cópia local ainda não possui commits. Acesso de escrita e administração não foram validados.

## Migrações

Não aplicável.

## Rollback

Remover apenas origin com git remote remove origin; isso não apaga arquivos locais ou o repositório no GitHub.

## Pendências

Revisar arquivos para o primeiro commit, enviar código, executar CI e configurar proteção da branch. Corrigir a cobertura insuficiente de 55,16% registrada na revalidação antes do aceite completo.
