# Identificadores exclusivos de entregador

## Objetivo e motivo

Exigir CPF válido e exclusivo e telefone exclusivo por entregador, preservando exclusividade de e-mail e placa. Requisições concorrentes devem retornar conflito sem persistência parcial.

## Escopo

Domínio, contrato HTTP de cadastro, serviço, modelo EF/migration, formulário web e testes. CPF somente no envio e armazenamento privado, sem devolução em respostas públicas. Telefone exclusivo dentro do conjunto de entregadores; e-mail permanece global.

## Impacto e migrações

CPF obrigatório para novo cadastro. Coluna nullable preserva entregadores legados sem inventar documentos. Normalização dos telefones legados e índice único; colisões existentes devem bloquear a migration e exigir revisão, sem exclusão automática.

## Validação

100 testes .NET aprovados (29 novos), build web aprovado e 18 testes desktop/mobile aprovados incluindo máscara/envio normalizado do CPF. Aceite HTTP/SQL base aprovado com nove respostas esperadas. Sete cenários concorrentes em PostgreSQL isolado aprovados: três de empresa e quatro de entregador (e-mail, telefone, CPF, placa). Cada par simultâneo gerou exatamente 201 + 409 e um usuário persistido. Banco e container temporários e dados fictícios foram removidos após os testes. Modelo EF sem diferenças pendentes e gate documental aprovado.

Migration conjunta `20260914171921_UniqueRegistrationIdentifiers` aplicada com sucesso no banco isolado. No banco da demonstração, a verificação prévia interrompeu a migration por um grupo de telefone duplicado com dois entregadores. Nenhum registro existente foi removido ou fundido; a API da demonstração permanece na versão anterior. O campo CPF já está no frontend local, mas a exclusividade nova no servidor só ficará ativa após solucionar o legado e atualizar a API.

Scripts `validate-registration-local.sh`, `validate-registration-local.py` e `validate-registration-uniqueness.py` usam banco exclusivo por execução no PostgreSQL local. O runner reinicia somente o container temporário entre cenários para respeitar o rate limit sem alterar a política da API.

## Rollback

Reverter aplicação e remover índices novos mediante migration revisada. Preservar coluna CPF e dados coletados; remover a coluna perderia esses dados. Não reverter nem apagar registros anteriores automaticamente.

## Pendências

Revisar qual telefone pertence a cada um dos dois registros legados antes de aplicar os índices na demonstração. Complementação de CPF dos cadastros legados, comprovação de titularidade e atualização do cliente mobile permanecem pendentes. Não expor CPF em respostas públicas ou mensagens de conflito. Downgrade gerado remove a coluna CPF: não executá-lo sobre dados coletados sem backup e revisão.
