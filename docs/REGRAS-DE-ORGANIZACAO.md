# Regras restritivas de organização e evolução

Este arquivo é normativo. Qualquer implementação deve obedecer a estas regras.

## 1. Registro obrigatório de mudança

Cada alteração deve criar um arquivo separado em `docs/mudancas/` antes ou junto da implementação. O nome deve seguir:

```text
AAAA-MM-DD-nn-titulo-curto.md
```

O arquivo deve informar objetivo, motivo, escopo, arquivos afetados, impacto, testes, migrações, rollback e tarefas pendentes. Não reutilizar o arquivo de outra mudança.

## 2. Motivo documentado no código

Toda regra de negócio, exceção, limite ou comportamento não óbvio introduzido deve conter comentário no código explicando o motivo e, quando aplicável, apontando para o registro em `docs/mudancas/`.

Formato mínimo:

```csharp
// Regra: motivo do comportamento e consequência operacional.
// Mudança: docs/mudancas/AAAA-MM-DD-nn-titulo-curto.md
```

Comentários não devem repetir o que o código já deixa evidente. Eles devem explicar o porquê, nunca apenas o quê.

## 3. Tarefas e status

- O backlog oficial é `docs/PLANO-DO-PROJETO.md`.
- Ao iniciar uma tarefa, marcar como `[~]` e registrar o arquivo da mudança.
- Ao concluir, marcar como `[x]` somente após testes e documentação.
- Tarefas bloqueadas devem indicar o motivo no registro da mudança.
- Nunca apagar uma tarefa concluída; preservar histórico.

## 4. Estrutura de código

- Separar domínio, aplicação, infraestrutura e apresentação.
- Não colocar regra de negócio em componentes React, controllers ou scripts de banco.
- Configurações operacionais devem estar centralizadas e auditáveis.
- DTOs não devem ser usados como entidades de persistência.
- Toda alteração de banco deve usar migration versionada.
- Segredos nunca entram no Git, documentação ou logs.

## 5. Testes obrigatórios

Uma mudança só pode ser concluída com testes proporcionais ao risco. Regras de tarifa, raio, licença, palestra, agrupamento, concorrência, cancelamento e permissões exigem testes automatizados.

## 6. Revisão obrigatória

Antes de considerar uma mudança concluída, verificar:

- registro separado criado;
- motivo documentado no código quando houver regra não óbvia;
- backlog atualizado;
- testes executados;
- migration e rollback avaliados;
- segurança e permissões verificadas;
- documentação de API e tela atualizada.

## 7. Proibições

- Não implementar comportamento importante sem registro de mudança.
- Não alterar uma regra existente sem explicar a razão e o impacto.
- Não apagar histórico para esconder uma decisão anterior.
- Não hardcodar raio, tarifa, prazo, validade ou limite operacional.
- Não publicar em produção sem validação local e autorização explícita.

## 8. Verificação automática da política

O script scripts/check-documentation.mjs exige um novo registro em docs/mudancas/ para alterações e confere as seções principais. O workflow .github/workflows/ci.yml executa essa verificação, compilação, testes, análise e cobertura mínima de 80% do código próprio. A revisão humana deve confirmar motivo, impacto, comentários e atualização do backlog; a automação não avalia a justificativa semântica.

Para restringir merges no servidor Git, os checks devem ser obrigatórios na proteção da branch. Essa configuração remota permanece pendente até existir repositório remoto definido. Registro desta implementação: docs/mudancas/2026-09-09-02-fase-zero.md.

## 9. Revisão de simplicidade antes de codificar

A [diretriz Ponytail](DIRETRIZ-PONYTAIL.md) é obrigatória antes de escrever ou alterar código. Registrar brevemente no arquivo da mudança o fluxo inspecionado, o reaproveitamento escolhido e a necessidade de novas dependências ou abstrações, quando houver. Não criar arquitetura especulativa nem comprimir código em prejuízo da legibilidade.

Esta verificação é de processo e revisão humana: o CI atual não consegue comprovar que o fluxo foi lido ou que a solução é a mais simples. Ponytail não permite dispensar testes, documentação, validação, segurança ou requisitos explícitos. Registro: [adoção](mudancas/2026-09-15-02-diretriz-ponytail.md).
