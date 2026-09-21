# Seletor do aceite de reabertura administrativa

## Objetivo e motivo

Corrigir o cenário de aceite PostgreSQL após a interface administrativa trocar o campo obrigatório `Motivo público` pela `Mensagem ao titular (opcional)`. O cenário real aguardava um rótulo inexistente e expirava antes de alcançar a decisão de reabertura.

## Escopo e escolha Ponytail

- O helper de decisão do Playwright foi ajustado para o rótulo atual da interface.
- O teste continua preenchendo uma mensagem fictícia nas decisões que abrem confirmação, validando histórico público e sem alterar o comportamento opcional ao usuário.
- O teto de 180 segundos foi preservado; não foram adicionados retries, pausas, mocks, dependências ou mudanças na API.

## Impacto e segurança

O ajuste é restrito ao cenário opt-in e ao banco isolado `coletas_phase1_acceptance`. Não modifica dados da demonstração, políticas de autenticação nem regras de reabertura.

## Validação

- Cenário desktop executado em 21/09/2026 com `COLETAS_REAL_ACCEPTANCE=1`, API isolada em 5081 e relatório Playwright `passed`.
- Confirmados cadastro, documentos, aprovação, bloqueio concorrente, rejeição, reabertura e aprovação final; o ambiente fictício foi removido pelo script de parada.

## Migração e rollback

Não há migration. Para rollback, restaurar o seletor antigo; isso fará o teste expirar porque esse rótulo não existe mais na interface.

## Pendências

Nenhuma pendência específica desta correção.
