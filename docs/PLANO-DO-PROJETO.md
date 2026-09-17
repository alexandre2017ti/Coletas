# Plano do projeto

## Objetivo

Criar uma plataforma de entregas e coletas para uma ou mais cidades. Estabelecimentos solicitam entregas pelo site; entregadores recebem ofertas no aplicativo Android/iOS conforme disponibilidade, localização, licença e regras operacionais.

## Usuários

- Administrador: configura regras, aprova cadastros, controla operação e acessa relatórios.
- Operador: acompanha entregas e intervém em problemas.
- Estabelecimento: cria, agrupa e acompanha entregas.
- Entregador: envia documentos, paga licença, participa de palestras, recebe e executa entregas.

## Fluxo principal

1. O estabelecimento informa coleta, destino, destinatário, telefone, volumes e observações.
2. A API calcula distância, rota, taxa e prazo.
3. O estabelecimento confirma a solicitação.
4. O sistema procura entregadores elegíveis no raio inicial configurado.
5. A oferta é enviada para um grupo limitado de entregadores.
6. O primeiro aceite confirmado vence por transação no banco.
7. O entregador atualiza coleta, deslocamento e entrega pelo aplicativo.
8. O destinatário confirma por PIN, foto, assinatura ou nome de quem recebeu.
9. Todos os eventos ficam registrados para auditoria.

## Regras de negócio iniciais

- Somente entregador disponível, aprovado, com licença vigente e capacitação válida pode receber ofertas.
- O raio inicial, raio máximo, quantidade de entregadores por rodada e tempo de aceite são configurações administrativas.
- O estabelecimento pode agrupar entregas quando os destinos estiverem dentro do raio configurável da primeira entrega; valor inicial sugerido: 5 km.
- Agrupamento também depende de prazo, capacidade do veículo, distância de rota e limite de paradas.
- O primeiro aceite válido confirma a oferta; aceites concorrentes devem ser rejeitados com segurança.
- Licenças devem ter período, pagamento, status e histórico; não usar apenas um campo booleano de ativação.
- Documentos devem possuir status `Pendente`, `Em análise`, `Aprovado`, `Reprovado`, `Vencido` ou `Bloqueado`.
- Localização deve ser enviada apenas quando necessária e em frequência configurável.

## Stack prevista

- API: C# com ASP.NET Core .NET 10.
- Site: React + TypeScript.
- Aplicativo: React Native + TypeScript.
- Banco: PostgreSQL + PostGIS.
- Cache, presença e ofertas temporárias: Redis.
- Tempo real: SignalR.
- Notificações: Firebase Cloud Messaging.
- Arquivos: Cloudflare R2 ou armazenamento compatível com S3.
- Infraestrutura: Ubuntu, Docker Compose, Cloudflare e proxy reverso.

## Módulos da API

1. Identidade, usuários e permissões.
2. Estabelecimentos.
3. Entregadores, veículos e documentos.
4. Licenças e pagamentos.
5. Palestras e presenças.
6. Entregas, paradas e rotas.
7. Ofertas e distribuição por geolocalização.
8. Localização em tempo real.
9. Comprovantes e eventos da entrega.
10. Notificações.
11. Configurações operacionais.
12. Auditoria e relatórios.

## Entidades previstas

`Users`, `Roles`, `Establishments`, `EstablishmentUsers`, `Couriers`, `CourierDocuments`, `Vehicles`, `Licenses`, `LicensePayments`, `Lectures`, `LectureAttendances`, `Deliveries`, `DeliveryStops`, `DeliveryOffers`, `DeliveryEvents`, `CourierLocations`, `Routes`, `Payments`, `ProofsOfDelivery`, `Notifications`, `SystemSettings` e `AuditLogs`.

## Backlog de implementação

### Fase 0 — Fundação

Registro: [2026-09-09-02-fase-zero.md](mudancas/2026-09-09-02-fase-zero.md).

Aceite integrado: [2026-09-09-03-validacao-integrada.md](mudancas/2026-09-09-03-validacao-integrada.md).

- [x] Criar solução .NET 10 e estrutura de módulos.
- [x] Criar frontend web React com TypeScript.
- [x] Criar aplicativo React Native — TypeScript e bundles Android/iOS verificados; não equivale a APK/IPA publicado.
- [x] Criar Docker Compose de desenvolvimento — build/up e aceite real aprovados no Ubuntu 24.04 WSL2.
- [x] Configurar PostgreSQL, PostGIS e Redis — consulta espacial, Redis PONG, prontidão e recuperação após queda validados.
- [x] Configurar migrations, dados iniciais e variáveis de ambiente seguras — aplicação e reexecução sem duplicação confirmadas no PostgreSQL; senha do teste somente em memória.
- [x] Criar CI, testes e análise de qualidade — aceite original da Fase 0 preservado; revalidação atual abaixo registra regressões e pendências.

Revalidação em 2026-09-10: [2026-09-10-04-revalidacao-fundacao.md](mudancas/2026-09-10-04-revalidacao-fundacao.md).

- [x] Corrigir e executar aceite Ubuntu/Docker após a Fase 1: JWT efêmero, duas migrations, idempotência, PostGIS, Redis e recuperação da API aprovados.
- [x] Recuperar cobertura mínima após inclusão da Fase 1 — validação local em 2026-09-10: 40 testes aprovados, 97,05% de linhas e 88,13% de branches; `scripts/check-coverage.ps1 -Minimum 0.9` aprovado. Registro: [2026-09-10-10-atualizacao-backlog-testes.md](mudancas/2026-09-10-10-atualizacao-backlog-testes.md).
- [x] Definir e vincular origin a https://github.com/alexandre2017ti/Coletas — registro [2026-09-10-05-repositorio-remoto.md](mudancas/2026-09-10-05-repositorio-remoto.md).
- [ ] Executar workflow no GitHub — alterações atuais ainda não foram enviadas após a reorganização da API e ampliação dos testes.
- [ ] Configurar checks obrigatórios na proteção da branch — pendentes branch remota e acesso administrativo.

Fase 0 teve aceite local em 2026-09-09; a revalidação acima informa o estado atual. Consulte [DESENVOLVIMENTO.md](DESENVOLVIMENTO.md). O remoto foi definido em 2026-09-10; execução do workflow e proteção de branch ainda estão pendentes. Publicação em servidor e lojas continua fora do aceite local.

### Interface — preparação visual transversal

Registro: [2026-09-10-01-interface-operacional.md](mudancas/2026-09-10-01-interface-operacional.md).

- [x] Interface web responsiva com FLOWSTACK, padrões visuais, exemplos identificados e testes de interação — auditoria Premium registrou limitação estática conhecida nos componentes Brick compostos.
- [ ] Integrar as telas a serviços reais, respeitando as fases abaixo.
- [ ] Adaptar a identidade ao React Native e homologar em Android/iOS reais.

Esta preparação visual não conclui funcionalidades de negócio das fases seguintes.

### Fase 1 — Acesso e cadastros

#### Situação consolidada em 2026-09-17

Esta seção atualiza o estado histórico abaixo, sem apagar evidências anteriores. Registro: [aceite integrado](mudancas/2026-09-17-01-aceite-fase-1.md). Roteiro: [homologação](HOMOLOGACAO-FASE-1.md).

- [x] Fluxo web real em PostgreSQL isolado: cadastrar, acessar, enviar documentos, analisar, aprovar e acessar conta liberada.
- [x] Correção, reprovação, bloqueio e concorrência administrativa 200/409 com um único evento persistido.
- [x] Login/onboarding, renovação, logout e documentos privados; testes de autorização, edição de veículo e versão documental.
- [x] Recuperação implementada e testada localmente: token de uso único, expiração, revogação e resposta genérica.
- [ ] Recebimento real de e-mail — usuário ainda não possui provedor SMTP.
- [x] Mobile adaptado ao CPF obrigatório e contratos de acesso/documentos; TypeScript, três testes e bundles Android/iOS aprovados.
- [ ] Homologação física Android — usuário possui aparelho USB; falta preparar ferramentas/cliente e executar o roteiro.
- [ ] Homologação física iOS — aparelho indisponível.
- [x] Demonstração atualizada em API 5080/site 5173, readiness 200 e cadastro existente preservado.
- [x] Tornar opcional a mensagem ao titular em todas as decisões administrativas — cada ação registra uma mensagem padrão quando o campo estiver vazio. [Registro](mudancas/2026-09-17-04-inicio-analise-sem-motivo.md).
- [x] Testes locais: 150 .NET, 90,28% de linhas; 72 web; aceite PostgreSQL separado aprovado.
- [~] Envio das mudanças locais e acompanhamento do workflow GitHub.
- [ ] Confirmar/configurar checks obrigatórios — proteção administrativa inacessível ao conector (403); consulta pública da branch informa protected=false.

A Fase 1 não está 100% homologada enquanto SMTP externo e aparelhos permanecerem pendentes. A prévia de entregas não conclui a Fase 2.

- [~] Integrar e homologar acesso, recuperação, documentos e administração após refatoração; adaptar mobile e revisar CI. [Rodada atual](mudancas/2026-09-16-03-integracao-acesso-administracao.md).

- [x] Itens 4 e 5: separar responsabilidades de perfil/documentos/bootstrap e unificar formulário/cliente HTTP — 145 testes .NET, cobertura de linhas 84,03%, 66 testes web desktop/mobile, build/lint e formatação aprovados. Validação local com EF InMemory e API simulada no navegador; não inclui publicação/homologação PostgreSQL. [Registro e limites da auditoria](mudancas/2026-09-16-02-servicos-e-cliente-http.md).

- [x] Corrigir itens 1, 2 e 3 da revisão: invalidação cadastral, remoção das decisões duplicadas e emissão única de JWT — 140 testes aprovados, cobertura de linhas 80,60% e formatação conferida. Validação EF InMemory/HTTP local; homologação PostgreSQL e atualização da demonstração continuam pendentes. [Registro](mudancas/2026-09-16-01-refatoracao-identidade.md).

- [~] Análise administrativa versionada, fila, motivos públicos/internos e proteção contra decisão desatualizada — [registro](mudancas/2026-09-15-01-analise-administrativa.md). Interface e homologação permanecem pendentes até validação.

- [x] Implementar e testar exclusividade de CNPJ/e-mail/telefone de empresas e CPF/e-mail/telefone/placa de entregadores — 100 testes .NET, 18 testes web e sete cenários concorrentes em PostgreSQL isolado aprovados. Registros: [empresa](mudancas/2026-09-14-03-identificadores-exclusivos-empresa.md) e [entregador](mudancas/2026-09-14-04-identificadores-exclusivos-entregador.md).
- [x] Ativar exclusividade na demonstração — cadastros antigos removidos com autorização, migration e índices conferidos, API 5080 atualizada e validação de CPF via proxy confirmada. [Limpeza autorizada](mudancas/2026-09-14-05-limpeza-cadastros-demo.md).
- [x] Adaptar cliente mobile ao CPF obrigatório nos novos cadastros — validado tecnicamente em 17/09; aceite físico separado acima.

- [x] Validar cadastros pela API real com PostgreSQL — migration aditiva aplicada e reexecutada, 9 respostas HTTP esperadas e consultas SQL aprovadas em 2026-09-14. [Registro](mudancas/2026-09-14-02-cadastro-postgres-real.md).
- [x] Homologar envio pelo navegador até o PostgreSQL — fluxo completo aprovado em banco isolado em 17/09; mesma versão publicada na demonstração sem inserir fixtures no banco existente.

- [x] Validar alinhamento, máscaras e edição da placa nos cadastros web — 18 testes desktop/mobile aprovados, build web aprovado; consulta CNPJ e envio exercitados com respostas simuladas. [Aceite dos formulários](mudancas/2026-09-14-01-aceite-formularios.md). Integração com API real permanece pendente.

Rodada em execução: [integração e aceite local](mudancas/2026-09-10-16-integracao-fase-1.md). Inclui bordas/foco/placeholders, envio real dos cadastros, sessão, recuperação de senha, documentos privados, veículo e administração. Nenhuma pendência abaixo será encerrada apenas por existir uma tela ou um teste simulado.

- [~] Login, recuperação e renovação segura de sessão — login JWT curto implementado; recuperação e renovação pendentes. Registro: [2026-09-10-03-fase-1-acesso-cadastros.md](mudancas/2026-09-10-03-fase-1-acesso-cadastros.md).
- [~] Perfis e autorização por função — perfis e política administrativa implementados; cobertura completa de autorização pendente.
- [~] Cadastro de estabelecimentos — endpoint e persistência implementados; interface e fluxo real pendentes.
- [~] Cadastro de entregadores — endpoint e persistência implementados; interface e validações externas pendentes.
- [~] Cadastro de veículos — criação junto do cadastro de entregador implementada; edição e validação externa pendentes.
- [~] Upload protegido de documentos — registro de metadados implementado; upload privado pendente.
- [~] Fluxo de aprovação e bloqueio — endpoints administrativos implementados; conta administrativa segura, auditoria e interface pendentes.

### Fase 2 — Entregas básicas

- [ ] Criar solicitação de coleta e entrega.
- [ ] Calcular distância, rota, taxa e prazo.
- [ ] Implementar máquina de estados da entrega.
- [ ] Exibir histórico e eventos.
- [ ] Criar painel operacional.
- [ ] Criar acompanhamento para o estabelecimento.

### Fase 3 — Distribuição

- [ ] Presença disponível/indisponível.
- [ ] Captura de localização com consentimento.
- [ ] Busca geográfica de entregadores elegíveis.
- [ ] Ofertas com expiração.
- [ ] Aceite concorrente protegido por transação.
- [ ] Notificações push Android/iOS.
- [ ] Atualizações em tempo real via SignalR.
- [ ] Substituição e cancelamento.

### Fase 4 — Regras comerciais e agrupamento

- [~] Configurações de tarifa — núcleo de cotação e adicional de volta implementados e testados; integração com entregas, persistência/auditoria e definição final do adicional por km pendentes. Registro: [2026-09-10-02-tarifa-volta-operacional.md](mudancas/2026-09-10-02-tarifa-volta-operacional.md).
- [ ] Licença semanal.
- [ ] Integração de pagamentos e webhooks idempotentes.
- [ ] Palestras e presença.
- [ ] Agrupamento dentro do raio configurável.
- [ ] Rota com múltiplas paradas.
- [ ] Limites por veículo e prazo.

### Fase 5 — Comprovação e operação

- [ ] PIN de entrega.
- [ ] Foto e identificação de quem recebeu.
- [ ] Registro de geolocalização e horário.
- [ ] Tratamento de ocorrências.
- [ ] Avaliações.
- [ ] Relatórios operacionais e financeiros.
- [ ] Logs de auditoria.

### Fase 6 — Produção e escala

- [ ] Backup automático e teste de restauração.
- [ ] Health checks e monitoramento.
- [ ] Rate limit e proteção de endpoints.
- [ ] Cloudflare, HTTPS e firewall.
- [ ] Separar banco da aplicação quando necessário.
- [ ] Testes de carga e concorrência.
- [ ] Procedimento de rollback.
- [ ] Publicação nas lojas Android e iOS.

## Infraestrutura inicial

Para o primeiro ambiente de produção: um servidor de aplicação com 4 vCPU, 8 GB RAM e SSD; banco PostgreSQL com backup externo obrigatório. Quando a carga justificar, separar aplicação, banco e Redis. Não adotar microsserviços ou Kubernetes antes de existir necessidade comprovada.

## Critérios gerais de aceite

- Aplicar a [diretriz Ponytail](DIRETRIZ-PONYTAIL.md) antes de escrever código, preservando legibilidade, segurança, testes e os padrões do projeto. [Registro da adoção](mudancas/2026-09-15-02-diretriz-ponytail.md).

- Toda regra configurável funciona sem alteração de código.
- A tarifa deve separar taxa mínima, adicional por quilômetro excedente e adicional de volta operacional; o adicional de volta é R$ 1,50 por solicitação quando aplicável.
- Toda mudança de estado gera evento auditável.
- Nenhuma entrega pode ser aceita por dois entregadores.
- Documentos e dados sensíveis não ficam públicos.
- Testes cobrem tarifas, raio, licença, palestra, agrupamento, cancelamento e concorrência.
- A funcionalidade possui documentação atualizada e registro de mudança.
