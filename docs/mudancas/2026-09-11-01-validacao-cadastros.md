# Validações de WhatsApp, placa e consulta CNPJ

## Objetivo e motivo
Atender aos limites e máscaras solicitados no cadastro. Rejeitar identificadores inválidos na API, além de orientar o preenchimento visual.

## Escopo e arquivos afetados
Registration.tsx, registrationValidation.ts, estilos de formulário, validador de domínio, IdentityService/ProfileService e testes. Ajuste mínimo da ambiguidade SecurityToken deixada pela interrupção anterior para recuperar compilação.

## Impacto
WhatsApp brasileiro aceita fixo (10 dígitos) ou celular (11), com DDD; +55 opcional na entrada e armazenamento somente de dígitos nacionais. Placas antigas/Mercosul normalizadas para sete caracteres. CNPJ tem validação dos dígitos verificadores numérica/alfanumérica. BrasilAPI consultada somente pelo botão, enviando apenas CNPJ numérico; nome/razão editáveis e cadastro manual disponível em erro ou formato não suportado pelo provedor. Resposta atrasada é descartada após edição/troca de tela. Máscara não prova titularidade, WhatsApp ativo ou regularidade do veículo/empresa.

## Fontes
https://github.com/BrasilAPI/BrasilAPI/blob/main/pages/docs/doc/cnpj.json
https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf

## Validação
Em execução: testes de limites, formatos, colagem e DV; Playwright de consultas/sucesso/falhas/concorrência; build e testes backend.

## Migrações
Esta validação não altera schema. A Fase 1 interrompida contém alterações de modelo anteriores ainda não homologadas; não confundir com esta correção.

## Rollback
Reverter os arquivos desta alteração preservando demais mudanças locais. Dados normalizados são compatíveis com campos existentes; nenhuma exclusão de dados.

## Pendências
Disponibilidade externa da BrasilAPI não é garantida; CNPJ alfanumérico usa cadastro manual. Fase 1 segue pendente de conclusão integrada.
