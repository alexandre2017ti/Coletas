# Adoção da diretriz Ponytail

## Motivo

Solicitação do usuário para incorporar Ponytail às skills e tornar sua avaliação de simplicidade obrigatória antes de escrever código, mantendo o projeto legível para manutenção humana.

## Escopo

Inspeção das skills principal e de revisão na revisão `e3ba2aa6f1e6f0bc4d69eb09c9f0d0a93af56156` de DietrichGebert/ponytail. Instalação local dessas duas skills; diretriz versionada em `docs/DIRETRIZ-PONYTAIL.md`, referenciada em AGENTS.md e nas regras de organização. Sem plugin completo, hooks, MCP, commit ou publicação.

## Avaliação de simplicidade

Reutilizar o instalador de skills existente e o AGENTS.md do projeto. Não criar um novo mecanismo de execução ou duplicar o conteúdo integral das skills. Acrescentar apenas a precedência necessária para preservar testes, validações, FLOWSTACK e a documentação já exigida.

## Impacto

Não altera regras de negócio nem banco de dados. A instalação local não instala skills nas máquinas dos demais colaboradores. O CI continua verificando documentação, mas não certifica compreensão do fluxo ou simplicidade semântica. Não há motivo para adicionar comentários ao código de execução nesta mudança documental.

## Validação

Inspeção das instruções remotas concluída; validação dos arquivos instalados e da política documental em execução. Não representa aceite da interface administrativa ainda em desenvolvimento.

## Rollback

Reverter apenas os trechos documentais desta mudança e remover as duas skills instaladas, caso solicitado, preservando demais skills e configurações. Não há migration.

## Pendências

Confirmar descoberta das skills no próximo turno. Revisão semântica obrigatória mantida no AGENTS.md mesmo quando a skill não estiver disponível.
