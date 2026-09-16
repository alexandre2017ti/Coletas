# Regras obrigatórias do repositório

Leia docs/REGRAS-DE-ORGANIZACAO.md, docs/PLANO-DO-PROJETO.md e docs/DECISOES-ARQUITETURAIS.md antes de alterar o projeto.
Cada mudança exige arquivo próprio em docs/mudancas/, com motivo, escopo, testes e rollback. Explique no código o porquê dos comportamentos não óbvios e referencie o registro. Atualize o backlog somente conforme a validação realmente executada. Nunca versionar segredos nem publicar sem autorização explícita.

## Antes de escrever ou alterar código — Ponytail

Leia docs/DIRETRIZ-PONYTAIL.md e aplique a skill `ponytail`, quando disponível, antes de escrever código. Entenda o fluxo e seus chamadores, procure implementação existente e escolha a menor mudança que cumpra os requisitos. Na revisão, use `ponytail-review` para identificar complexidade desnecessária; ela não substitui revisão de correção ou segurança.

As regras do Coletas prevalecem sobre simplificações genéricas: preservar testes, validações, acessibilidade, documentação, stack, limites configuráveis e componentes FLOWSTACK. Legibilidade e manutenção humana valem mais que contagem de linhas. Não remover funcionalidades solicitadas nem alterações do usuário. Se a skill não estiver disponível, aplicar a diretriz versionada e informar a limitação; não executar instaladores ou hooks externos automaticamente.
