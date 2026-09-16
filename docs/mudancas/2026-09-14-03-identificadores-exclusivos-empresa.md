# Identificadores exclusivos de empresa

## Objetivo e motivo

Cada empresa deve possuir CNPJ, e-mail e telefone exclusivos. CNPJ e e-mail já têm índices únicos; faltava telefone exclusivo entre estabelecimentos e validação mais rigorosa do formato do e-mail.

## Escopo

Validação e normalização no serviço, índice único no telefone do estabelecimento, migration de normalização dos telefones legados, tratamento de conflito concorrente e testes HTTP/SQL. E-mail continua exclusivo entre todas as contas; telefone é exclusivo entre empresas, sem introduzir restrição entre entregadores e empresas.

## Validação

100 testes .NET e 18 testes web aprovados no conjunto desta mudança e da mudança de entregador. Testes HTTP independentes por identificador e três cenários concorrentes de empresa aprovados em PostgreSQL isolado: e-mail, telefone e CNPJ resultaram em 201 + 409 com exatamente um usuário persistido. Erros públicos não expõem dados de outras empresas.

## Impacto e migrações

Migration conjunta `20260914171921_UniqueRegistrationIdentifiers` revisada e aprovada em banco isolado. O preflight detectou duplicidade em dois entregadores da demonstração e impediu aplicar a migration nesse banco; não há grupos duplicados de telefone entre empresas. Não fundir, excluir ou trocar contatos automaticamente. Validação de formato não comprova titularidade de telefone/e-mail.

## Rollback

Remover o índice novo por migration revisada e reverter validação do serviço, preservando registros e histórico. A normalização remove máscaras e prefixo internacional; não recupera apresentação original.

## Pendências

Revisão dos contatos legados e atualização da demonstração. CPF/telefone são normalizados na entrada, e-mail ignora caixa e espaços externos; não remover pontos ou tags do e-mail, pois isso pode unir endereços distintos.
