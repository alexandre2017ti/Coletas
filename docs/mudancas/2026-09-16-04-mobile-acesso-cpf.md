# Mobile: cadastro e acesso

## Motivo e escopo
Substituir a tela inicial de preparação pelo cadastro/login/conta do entregador. CPF obrigatório, máscaras, validação compartilhada com a web e conexão com contratos atuais. Reutilizar ui.tsx, Expo e bibliotecas instaladas; não adicionar dependências.

## Segurança
Tokens apenas em memória. URL da API configurada por ambiente, HTTPS obrigatório fora do desenvolvimento. Recuperação via API sem confirmar existência de conta; documentos enviados privadamente e alteração exige novo login. Não registrar documentos, senhas ou tokens.

## Validação e testes
Em 17/09: TypeScript aprovado; bundles Android (596 módulos) e iOS (598 módulos) gerados; três testes do transporte aprovados. Exportação não é APK/IPA. Compatibilidade online Expo e testes físicos continuam pendentes; usuário dispõe de Android por USB, mas adb não foi encontrado no PATH nem nos caminhos padrão. Sem iPhone disponível.

## Impacto e migração
Sem migration. O validador puro existente é movido para apps/shared com reexport web, evitando dois conjuntos divergentes de regras de CPF/telefone/placa.

## Rollback
Reverter a tela mobile e os imports compartilhados, preservando backend e dados. Nenhuma exclusão de dados.
