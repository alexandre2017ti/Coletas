# Validação integrada da Fase 0

Status: concluída em 2026-09-09.

## Objetivo e motivo
Encerrar o aceite da fundação executando containers Linux, migration, PostGIS, Redis e HTTP real. A validação anterior não possuía engine funcional.

## Escopo
Preparar runtime local isolado para Coletas, executar o Compose, corrigir problemas encontrados e atualizar o backlog com evidências. Não alterar servidores remotos ou cadastros comerciais.

Arquivos: .gitattributes, compose.yaml, infra/api.Dockerfile, infra/web.Dockerfile, scripts/prepare-linux-runtime.sh, scripts/validate-compose.sh, scripts/validate-compose.ps1, scripts/docker.ps1, .github/workflows/ci.yml e documentação de desenvolvimento/plano/decisões.

## Ambiente instalado

- Ubuntu 24.04 LTS no WSL2 local, distribuição coletas-dev.
- Disco: C:/Users/Patricia_/AppData/Local/Coletas/wsl/ext4.vhdx.
- Docker Engine 29.1.3, Compose 2.40.3, Buildx 0.30.1.
- Imagem WSL obtida em https://cloud-images.ubuntu.com/wsl/releases/noble/current/ e validada contra SHA256SUMS por HTTPS. O instalador padrão retornou HTTP 503 ao consultar o catálogo.
- O registro podman-uosserver permaneceu intacto.

## Mudanças e motivos

- Biblioteca libgssapi-krb5-2 incluída no runtime .NET: Npgsql emitia aviso de libgssapi_krb5.so.2 ausente, apesar de completar a conexão por senha.
- Bibliotecas Alpine atualizadas: scanner encontrou libssl3/libcrypto3 3.5.7-r0 com correção 3.5.8-r0.
- npm removido após npm ci no build web; Vite é iniciado diretamente pelo Node. O gerenciador não é necessário para executar esse container e continha dependências transitivas vulneráveis mesmo após testar npm 12.0.2. Instalação local via npm e lockfile do projeto continuam iguais.
- Limites de memória/CPU no Compose para o ambiente local.
- Roteiro compartilhado pelo CI e WSL usa senha em memória e namespace exclusivo; valida banco, cache, HTTP, restart e indisponibilidade/recuperação do Redis.
- CI ganhou scanning HIGH/CRITICAL com correção disponível para as imagens próprias, usando Trivy action fixada por SHA verificado. A skill devops-engineer orientou limites, verificação pós-inicialização e scanning.

## Validação
Aceite integrado executado com sucesso e repetido após as correções das imagens, incluindo queda/recuperação do Redis. Última execução: namespace coletas-validation-1788983898-585, exit code 0, mensagem VALIDATION PASSED.

- Build Linux de API, serviço migrate e web.
- InitialFoundation aplicada; uma entrada no histórico.
- Seed DeliveryGrouping.RadiusMeters=5000; uma linha, inclusive após reexecutar migrate.
- PostGIS 3.5 ativo; ST_DWithin verdadeiro para ponto coincidente e falso para ponto distante mais de 5 km.
- Redis PONG.
- API /health/ready e proxy web /health/ready: HTTP 200.
- Identificação /api/v1/platform: Coletas / Fundação.
- Após restart da API: readiness retorna 200 novamente.
- Redis parado: readiness 503 enquanto liveness permanece 200; após recuperação do Redis, readiness volta a 200.
- Containers e volumes de teste removidos ao terminar; somente dados descartáveis do roteiro.
- Scanner Trivy 0.70.0: imagens finais API e web com zero HIGH/CRITICAL com correção disponível. Não equivale a ausência de toda vulnerabilidade ou revisão de produção.
- Bibliotecas GSSAPI verificadas na imagem corrigida, sem o aviso anterior na reexecução.
- Script de documentação, sintaxe bash, YAML e wrapper PowerShell verificados. Arquivos .sh fixados em LF para futuros checkouts Windows.

## Migrações e rollback
Reutilizar InitialFoundation. Os recursos novos serão identificados como Coletas. Parar Compose sem -v preserva o volume. Não remover registros WSL existentes ou dados de outras aplicações.

O roteiro de aceite é a exceção explícita: usa nome coletas-validation-<timestamp>-<pid> e remove o volume que ele próprio criou. As imagens e o runtime ficam disponíveis para os próximos builds. Para encerrar somente o Ubuntu do projeto, usar wsl --terminate coletas-dev; não usar wsl --unregister, pois apaga o disco.

## Pendências
Nenhuma pendência do aceite local da Fase 0. Workflow remoto/proteção de branch aguardam definição do remoto; publicação e testes de aparelhos/lojas continuam nas fases correspondentes. A próxima implementação é a Fase 1.
