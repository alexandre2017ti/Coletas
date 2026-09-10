# Primeiro commit: revisão da base em desenvolvimento

## Objetivo e motivo

Versionar e enviar ao repositório Coletas a fundação, demonstração web, cotação e implementação parcial da Fase 1, conforme pedido explícito do usuário. Este checkpoint não representa aprovação para produção.

## Escopo e arquivos afetados

Primeiro snapshot dos arquivos de código, assets, configurações sem segredos, testes e documentação existentes; inclusão deste relatório. Não alterar regras de aplicação nesta revisão.

Em docs/DECISOES-ARQUITETURAIS.md, substituir quebras Markdown com dois espaços por barra invertida, preservando a apresentação e permitindo git diff --cached --check sem erros de whitespace.

## Revisão e prioridades

- P1: Program.cs valida assinatura e expiração JWT, mas não consulta o status atual da conta. SetUserStatusAsync bloqueia novos logins, porém tokens existentes permanecem utilizáveis. Implementar revogação/verificação de status com teste de token emitido antes do bloqueio.
- P1: SetUserStatusAsync permite ativar entregador sem verificar CNH, documento e regularidade do veículo. A implementação não atende ainda aos requisitos de aprovação. Adicionar validação e testes antes de operação real.
- P1: login nega contas Pending e documentos exigem autenticação; falta um acesso restrito para concluir cadastro e acompanhar análise. Criar fluxo de onboarding com autorização limitada.
- P2: limiter auth é compartilhado por todos os clientes (10 requisições/minuto). Particionar e configurar limites; testar isolamento entre clientes e resposta 429.
- P2: cadastro consulta unicidade antes de salvar, mas conflitos concorrentes não são convertidos de violação de índice em resposta controlada. Tratar conflito e testar concorrência.
- P2: validar limites de campos conforme schema e regras de senha/BCrypt, inclusive entradas longas e Unicode. Existem validações superficiais de e-mail, placa e documento.
- P2: cobertura registrada em 55,16% na revalidação anterior, abaixo de 80%; o CI deve continuar acusando falha até corrigir os testes.

Pontos positivos: camadas separadas, valores monetários decimal, chave JWT externa, migrations explícitas, índices únicos e cancellation tokens no acesso ao banco. Nenhuma correção de estilo solicitada.

Parecer: alterações necessárias antes do aceite da Fase 1/produção. O envio solicitado é um checkpoint de desenvolvimento com limitações documentadas. Não há escolha adicional necessária para esse envio.

## Validação

- Backend compilado com 0 avisos e erros; 15 testes aprovados nesta revisão.
- Web: npm run lint e npm run build aprovados.
- Inventário Git inspecionado: sem node_modules, bin, obj, dist, TestResults ou arquivos .env reais entre os candidatos.
- Busca por padrões comuns de tokens e chaves privadas nos candidatos não encontrou ocorrências; essa busca não é garantia de ausência de todo tipo de segredo.
- Remoto consultado sem referências existentes. Envio e conferência do hash remoto serão realizados após o commit.
- Não repetidos nesta revisão: navegador, bundles mobile, cobertura ou Docker; evidências anteriores permanecem nos registros próprios.

## Impacto e migrações

Envio do código ao GitHub poderá iniciar o workflow existente. Não há deployment de aplicação nem migration nova nesta revisão. As migrations existentes não são aplicadas em produção pelo envio.

## Rollback

Preservar histórico e usar commit de reversão para alterações futuras. Não usar force push ou apagar o repositório para desfazer o checkpoint.

## Pendências

Corrigir achados, obter CI aprovado e configurar proteção da branch. Confirmar envio pelo hash do commit remoto.
