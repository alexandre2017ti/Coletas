# Reabrir análise de cadastro rejeitado

## Objetivo e motivo

Permitir que um administrador corrija uma rejeição feita por engano sem criar outro cadastro, violar a unicidade de CPF/CNPJ, e-mail, telefone ou placa, nem apagar o histórico administrativo.

## Escopo e escolha Ponytail

- Foi reutilizada a máquina de estados `RegistrationReview`, o serviço de decisão versionada e a tela administrativa existentes.
- A nova ação `Reopen` só é válida quando o cadastro está `Rejected`; ela o move para `InReview` e exige uma nova aprovação explícita.
- Não há migration, novo endpoint, nova tabela, dependência ou abstração: o endpoint de decisões existente já recebe ações tipadas e registra o evento auditável.
- A mensagem ao titular é opcional. Sem texto informado, a API registra `Cadastro reaberto para nova análise.`.

## Impacto e segurança

- A rejeição original continua no histórico; a reabertura cria outro evento com versão posterior.
- A versão exibida ainda é obrigatória, portanto duas pessoas não conseguem reabrir ou aprovar a mesma versão simultaneamente.
- Reabrir não libera a conta: o estado volta a `Em análise` e somente `Approve` torna o acesso ativo.

## Validação

- Teste xUnit cobre a transição rejeitado → reaberto → aprovado, status, histórico e mensagem padrão.
- Teste Playwright desktop/mobile cobre o botão, a confirmação, a requisição versionada e o retorno visual para `Em análise`.
- O cenário PostgreSQL de aceite foi estendido para enviar `Reopen` após uma rejeição.

## Migração e rollback

Não há alteração de esquema ou migration. Para rollback, remover `Reopen` do enum, das transições, da interface e dos testes; eventos já gravados permanecem auditáveis como registros históricos.

## Pendências

Homologar o cenário PostgreSQL atualizado e atualizar a demonstração somente depois dos testes locais aprovados.
