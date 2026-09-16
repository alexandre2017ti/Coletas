# Cadastros de estabelecimento e entregador

## Objetivo e motivo
Disponibilizar na interface web os dois cadastros já suportados pela API, para iniciar a Fase 1 com um fluxo que o usuário possa testar.

## Escopo e arquivos afetados
Adicionado `apps/web/src/components/Registration.tsx`, navegação para os dois cadastros em `apps/web/src/App.tsx` e estilos responsivos em `apps/web/src/App.css`.

## Impacto
Os formulários enviam JSON aos endpoints `/api/v1/auth/register/establishments` e `/api/v1/auth/register/couriers`. A URL da API é configurável por `VITE_API_URL` e usa `http://localhost:5180` como padrão de desenvolvimento. A interface não armazena senha nem documentos; mostra erro de conexão quando a API não está disponível.

## Validação
Executar `npm run build`, `npx tsc --noEmit` e os testes Playwright existentes. O fluxo real depende da API disponível e de CORS configurado no ambiente.

## Migrações
Não aplicável; não houve alteração de banco.

## Rollback
Remover `Registration.tsx`, as entradas de navegação/estilos e este registro.

## Pendências
Adicionar CORS controlado para o domínio web, máscaras/validação específica de documento e telas de aprovação/login integradas.
