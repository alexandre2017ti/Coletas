# Diretriz Ponytail — simplicidade com segurança

## Objetivo

Manter o Coletas compreensível e sustentável por quem escreve e mantém o código. Aplicar antes de implementações, correções e refatorações; não usar como justificativa para entregar menos que o solicitado.

## Origem e instalação

- Origem: https://github.com/DietrichGebert/ponytail
- Revisão inspecionada: `e3ba2aa6f1e6f0bc4d69eb09c9f0d0a93af56156`.
- Skills selecionadas: `skills/ponytail` e `skills/ponytail-review`.
- Destino local: diretórios `ponytail` e `ponytail-review` em `$CODEX_HOME/skills` (nesta máquina, `C:/Users/Patricia_/.codex/skills`).
- Instalação somente de instruções. Não instalar plugin completo, hooks, MCP ou alterar configuração global de outros agentes como efeito desta diretriz.
- Outros colaboradores precisam instalar as skills em seu ambiente. Este documento e o AGENTS.md permanecem disponíveis no repositório mesmo sem elas.

## Procedimento obrigatório antes de codificar

1. Ler o requisito, as regras do projeto e os arquivos envolvidos. Rastrear os chamadores e o fluxo real antes de escolher a correção.
2. Confirmar o que precisa existir agora. Não criar funcionalidades, infraestrutura ou abstrações para uma necessidade hipotética.
3. Procurar serviços, validadores, componentes, tipos e testes existentes; reutilizá-los quando atendem ao contrato.
4. Verificar recursos da biblioteca padrão, da plataforma e das dependências já instaladas, preservando os padrões do projeto.
5. Implementar a menor mudança correta, legível e testável. Nova dependência ou abstração precisa de necessidade concreta registrada.
6. Revisar o diff quanto a duplicação e complexidade desnecessária, além da revisão normal de correção, segurança e testes.

Documentar sucintamente as escolhas no registro próprio da mudança. Não é necessário criar uma classe ou comentário para repetir cada etapa desta lista.

## Limites e precedência no Coletas

- Simplicidade não é código comprimido. Não juntar instruções nem eliminar nomes claros apenas para reduzir linhas.
- Manter separação entre domínio, aplicação, infraestrutura e API; reaproveitar interfaces existentes quando fazem parte da arquitetura aprovada.
- Não retirar testes xUnit, Playwright, cenários de concorrência ou cobertura exigida. A sugestão genérica do Ponytail de apenas um teste não substitui nossa política.
- Nunca reduzir validação de e-mail à presença de `@`, nem relaxar CPF, CNPJ, telefone, placa, unicidade, autorização ou validação no servidor.
- Não substituir componentes FLOWSTACK padronizados por controles nativos só por terem menos linhas. Acessibilidade, máscaras, estados e contratos visuais continuam obrigatórios.
- Preservar configurações operacionais solicitadas: tarifas, raio, licença e capacitação não são flexibilidade especulativa.
- Preservar registros em `docs/mudancas/`, backlog honesto, comentários do porquê e referências de mudança para comportamentos não óbvios.
- Nenhum comando externo, hook, remoção de arquivos, atualização de dependências ou publicação fica autorizado apenas pela leitura da skill.
- Não adotar modo `ultra` nem aplicar exclusões automaticamente. `ponytail-review` produz sugestões; remoções continuam limitadas ao escopo autorizado e precisam de validação.
- Continuar explicando o código quando o usuário pedir. O objetivo é facilitar manutenção humana, não limitar o aprendizado.

## Evidência e atualização

A diretriz é normativa; a conferência de simplicidade depende de revisão, não de uma garantia automática do CI. Claims de economia ou segurança do projeto externo não são garantias para o Coletas. Atualizar a revisão instalada somente após nova inspeção e registro da mudança.
