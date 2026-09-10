# Mudança 2026-09-10-02 — Adicional de volta operacional na tarifa

## Objetivo

Registrar a proposta de cálculo da tarifa de entrega com taxa mínima, adicional por quilômetro excedente e adicional fixo quando a operação exigir uma volta ao estabelecimento.

## Motivo

Algumas entregas exigem que o entregador retorne ao estabelecimento para entregar troco, devolver ou buscar uma maquininha de cartão, devolver casco ou executar outra atividade operacional de retorno. Esse deslocamento gera custo adicional e deve ser cobrado do estabelecimento de forma explícita.

A proposta busca manter uma cobrança simples e previsível, sem misturar o custo do retorno com a distância principal da entrega.

## Regra proposta

```text
ValorTotal =
  R$ 7,50
  + máximo(0, DistânciaDaRotaEmKm - 1,5) × AdicionalPorKm
  + (ExigeVoltaOperacional ? R$ 1,50 : R$ 0,00)
```

Parâmetros configuráveis:

```text
Taxa mínima: R$ 7,50
Franquia de distância: 1,5 km
Adicional por km: entre R$ 1,20 e R$ 1,50
Adicional de volta operacional: R$ 1,50
```

O adicional de volta é cobrado uma única vez por solicitação, independentemente da quantidade de motivos de retorno informados. A cobrança só deve ocorrer quando a solicitação for marcada e validada como exigindo retorno operacional.

## Exemplos

| Distância da rota | Sem volta | Com volta |
|---:|---:|---:|
| Até 1,5 km | R$ 7,50 | R$ 9,00 |
| 5 km, a R$ 1,20/km | R$ 11,70 | R$ 13,20 |
| 7 km, a R$ 1,35/km | R$ 14,93 | R$ 16,43 |
| 7 km, a R$ 1,50/km | R$ 15,75 | R$ 17,25 |
| 10 km, a R$ 1,50/km | R$ 20,25 | R$ 21,75 |

## Escopo

- Atualizar a diretriz de tarifa do projeto.
- Modelar a volta como adicional configurável, separado da distância.
- Exibir a composição da tarifa para o estabelecimento antes da confirmação.
- Preservar a tarifa calculada e os parâmetros utilizados para auditoria.
- Cobrar o adicional no valor do estabelecimento; o repasse ao entregador e a comissão da plataforma ainda precisam ser definidos.

## Fora do escopo desta mudança

- Integração da cotação com o fluxo de criação e confirmação de uma entrega.
- Definição final do adicional por quilômetro entre R$ 1,20 e R$ 1,50.
- Integração de pagamentos.
- Regras de pedágio, espera, área especial, horário de pico ou cancelamento.

## Arquivos afetados

- `docs/PLANO-DO-PROJETO.md`
- `docs/mudancas/2026-09-10-02-tarifa-volta-operacional.md`
- `src/Coletas.Domain/Pricing/TariffPolicy.cs`
- `src/Coletas.Application/Pricing/ITariffQuoteService.cs`
- `src/Coletas.Infrastructure/Pricing/TariffOptions.cs`
- `src/Coletas.Infrastructure/Pricing/TariffQuoteService.cs`
- `src/Coletas.Infrastructure/DependencyInjection.cs`
- `src/Coletas.Api/Program.cs`
- `src/Coletas.Api/appsettings.json`
- `tests/Coletas.Tests/FoundationTests.cs`
- `docs/DESENVOLVIMENTO.md`

O código de domínio contém comentário explicando a separação da volta operacional e referencia este registro. A configuração inicial de R$ 1,35 por quilômetro fica em `appsettings.json` e pode ser substituída por configuração de ambiente sem alterar o código.

## Impacto

- O preço apresentado ao estabelecimento poderá aumentar R$ 1,50 quando houver retorno operacional.
- A cotação deverá distinguir distância, adicional por quilômetro e volta.
- A regra deve utilizar `decimal` para valores monetários e configuração centralizada no backend.
- O adicional não deve ser aplicado automaticamente apenas por observação livre; deverá existir uma indicação estruturada e auditável.

## Validação

- `git diff --check`: aprovado.
- `scripts/check-documentation.mjs`: aprovado.
- `dotnet build Coletas.slnx --no-restore`: aprovado, 0 avisos e 0 erros.
- `dotnet test Coletas.slnx --no-build --no-restore --verbosity normal`: aprovado, 13 testes.
- A prontidão continua retornando `503` no teste que usa dependências deliberadamente indisponíveis; isso é o comportamento esperado do teste de fundação.
- Não foram executados Docker, migration nova, frontend ou fluxo de entrega real nesta mudança.

## Testes exigidos para implementação

- Distâncias de 0 km, 1 km e 1,5 km respeitam a taxa mínima.
- Distâncias acima de 1,5 km aplicam o adicional somente ao excedente.
- A volta adiciona exatamente R$ 1,50 uma única vez.
- Ausência de volta não adiciona valor.
- A combinação de volta com taxa mínima e com distância excedente é calculada corretamente.
- Arredondamento monetário ocorre em centavos e não usa `double` ou `float`.
- A cotação persiste os parâmetros e a versão da regra utilizados.
- Alterações administrativas dos parâmetros não modificam cotações já confirmadas.
- Usuário sem permissão não consegue ativar ou alterar o adicional.

## Migração

Não aplicável nesta etapa: os parâmetros são lidos de configuração fortemente tipada (`Tariff`) e o endpoint implementado apenas gera cotação. A implementação futura deverá avaliar migration para parâmetros administrativos, versão da regra, cotações confirmadas e campo estruturado de retorno operacional.

## Rollback

Reverter a diretriz deste registro e remover a referência adicionada ao backlog. Se já houver implementação, desativar o parâmetro de volta operacional por configuração, sem apagar cotações ou eventos históricos.

## Pendências

- Definir o valor inicial oficial do adicional por quilômetro.
- Definir quais motivos permitem a marcação de volta e se exigem aprovação do operador.
- Definir o repasse do adicional ao entregador.
- Implementar tela, persistência da cotação/regra e testes de integração com o fluxo real de entrega.
- Validar a política com simulação de margem e histórico de pedidos.
