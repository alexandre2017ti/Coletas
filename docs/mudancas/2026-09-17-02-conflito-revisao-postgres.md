# Conflito de revisão no PostgreSQL

## Motivo
O aceite real com duas decisões simultâneas retornou 200/500. O índice único do histórico rejeitou a segunda inserção antes do UPDATE concorrente do usuário. Os serviços já tratavam DbUpdateConcurrencyException, mas não esse caminho equivalente do PostgreSQL.

## Escopo
ColetasDbContext traduz exclusivamente SQLSTATE 23505 do índice IX_ReviewEvents_UserId_Version para DbUpdateConcurrencyException. Reutiliza o tratamento 409 existente de decisão, documento e alteração cadastral, sem duplicar filtros em cada serviço. Não converter outras violações de unicidade.

## Impacto e migração
Sem mudança de schema. A transação EF continua revertendo a tentativa perdedora; nenhuma decisão ou evento parcial é aceito. A resposta passa a orientar recarregamento.

## Validação e testes
Reprodução registrada no teste phase1-real.spec.ts: duas requisições com a mesma versão produziram exatamente 200/409 e um único evento no PostgreSQL. Aceite completo aprovado em 17/09; 150 testes .NET aprovados.

## Rollback
Reverter a tradução no contexto; preserva dados e índices. Retorna o erro 500 conhecido em concorrência, portanto não recomendado antes de correção alternativa.
