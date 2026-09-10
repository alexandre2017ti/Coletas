# Decisões arquiteturais

## DA-001 — Monólito modular inicial

**Data:** 2026-09-09\
**Status:** Aceita\
**Registro:** `docs/mudancas/2026-09-09-01-fundacao-documental.md`

Começar com uma API única em ASP.NET Core .NET 10, organizada em módulos internos. O objetivo é reduzir custo operacional e permitir evolução sem introduzir complexidade de microsserviços antes da necessidade real.

## DA-002 — PostgreSQL com PostGIS

**Data:** 2026-09-09\
**Status:** Aceita\
**Registro:** `docs/mudancas/2026-09-09-01-fundacao-documental.md`

Usar PostgreSQL como banco principal e PostGIS para consultas por distância e posição. Redis será auxiliar, nunca a fonte definitiva das entregas.

## DA-003 — React web e React Native móvel

**Data:** 2026-09-09\
**Status:** Aceita\
**Registro:** `docs/mudancas/2026-09-09-01-fundacao-documental.md`

Usar React + TypeScript no site e React Native + TypeScript no aplicativo, mantendo o backend em C#/.NET. A decisão favorece reaproveitamento da base frontend e suporte às necessidades de mapas, push e câmera.

## DA-004 — Fundação técnica reproduzível

**Data:** 2026-09-09\
**Status:** Implementada e validada localmente em containers Linux\
**Registro:** docs/mudancas/2026-09-09-02-fase-zero.md

Quatro projetos .NET separam domínio, aplicação, infraestrutura e API. Expo inicializa React Native. EF Core e Npgsql 10 utilizam migration explícita e configuração por ambiente; Redis não é fonte definitiva de dados. Lockfiles, compilação sem avisos, testes HTTP, cobertura mínima e testes do site fazem parte do CI. A política de documentação possui verificação automática e revisão humana do motivo no código.

Aceite concluído em docs/mudancas/2026-09-09-03-validacao-integrada.md. Runtime local: Ubuntu 24.04 WSL2 coletas-dev, Docker Engine e Compose. As configurações Ubuntu/Docker previstas para hospedagem continuam mantidas.
