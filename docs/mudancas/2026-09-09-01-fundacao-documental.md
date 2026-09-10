# Mudança 2026-09-09-01 — Fundação documental do aplicativo

## Objetivo

Criar a documentação inicial do aplicativo de entregas e coletas e estabelecer um processo restritivo para que tarefas, decisões e alterações futuras permaneçam rastreáveis.

## Motivo

O projeto precisa de uma fonte única para o escopo, backlog e regras operacionais. Como a plataforma terá regras sensíveis de geolocalização, licença, palestras, pagamentos e agrupamento, alterações sem histórico podem produzir comportamentos inconsistentes e difíceis de auditar.

## Escopo

- Criar plano do produto, arquitetura, módulos, entidades e backlog.
- Criar regras obrigatórias de organização.
- Criar formato de registro individual por mudança.
- Registrar decisões arquiteturais iniciais.

## Arquivos criados

- `README.md`
- `docs/PLANO-DO-PROJETO.md`
- `docs/REGRAS-DE-ORGANIZACAO.md`
- `docs/DECISOES-ARQUITETURAIS.md`
- `docs/mudancas/2026-09-09-01-fundacao-documental.md`

## Impacto

Nenhum código executável foi alterado. O backlog passa a ser a referência para implementação. A partir desta mudança, toda alteração deverá possuir registro próprio em `docs/mudancas/` e justificar no código as regras de negócio não óbvias.

## Testes e validação

- [x] Conferência da estrutura inicial do repositório.
- [x] Conferência dos links e caminhos da documentação.
- [x] Conferência de que o registro é separado dos documentos normativos.

## Rollback

Como esta mudança apenas adiciona documentação, a reversão consiste em remover os arquivos criados por este registro. A remoção deve ser feita apenas em uma mudança posterior, com justificativa própria.

## Tarefas pendentes

Todas as tarefas técnicas permanecem pendentes e estão listadas em `docs/PLANO-DO-PROJETO.md`.
