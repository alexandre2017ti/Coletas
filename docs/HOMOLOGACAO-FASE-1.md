# Homologação da Fase 1

Registro: [aceite local](mudancas/2026-09-17-01-aceite-fase-1.md).

## Rotas e acesso

- Portal: `http://127.0.0.1:5173/?view=login`.
- Conta: `?view=account`; fila administrativa: `?view=admin-reviews`.
- Recuperação: `?view=recovery`; redefinição: `?view=reset-password#token=...`.
- Tokens somente em memória; recarregar exige login. O fragmento de recuperação é removido da barra após leitura.
- Conta pendente acessa apenas o cadastro. Alterar veículo/documentos exige nova análise e revoga sessões. Bloqueio impede acesso.

### Primeiro administrador

Somente o responsável deve escolher o e-mail e digitar a senha no console interativo. Não enviar a senha à IA nem colocá-la na linha de comando. O bootstrap recusa criar outro administrador se já existir um.

```powershell
wsl -d coletas-dev -- docker exec -it coletas-preview-api-1 dotnet Coletas.Api.dll --bootstrap-admin
```

Os administradores dos testes são fixtures do banco isolado, não contas disponíveis na demonstração.

## Recuperação por e-mail — pendente de provedor

Em 17/09 o responsável informou não possuir SMTP. Não há envio externo homologado. Testes capturam o token apenas em memória e comprovam uso único, validade e revogação; não comprovam recebimento numa caixa real.

Após escolher o serviço, configurar privadamente `RECOVERY_SMTP_HOST`, `RECOVERY_SMTP_PORT`, `RECOVERY_SMTP_USERNAME`, `RECOVERY_SMTP_PASSWORD`, `RECOVERY_SMTP_FROM` e `RECOVERY_RESET_URL`. Compose mapeia para `RecoveryMail`. TLS é obrigatório. ResetUrl precisa ser HTTPS, apontar para a tela `?view=reset-password` e não conter fragmento. Não mostrar `docker inspect`/`compose config` completos com segredos.

Aceite externo: solicitar para conta própria, receber mensagem, abrir link, trocar senha, confirmar rejeição da senha antiga e do link reutilizado, confirmar revogação das sessões. Registrar só resultados, nunca conteúdo/token da mensagem. Confirmar endereço remetente e regras de envio com o provedor escolhido.

## Android físico e iOS

O responsável possui Android; iPhone não está disponível. Nenhum aceite físico foi concluído. TypeScript, testes de transporte e bundles não equivalem a teste de APK/IPA ou aparelho.

O Compose expõe a API somente no computador. Não usar `localhost` do telefone como se fosse o computador. Preferir USB com `adb reverse tcp:5080 tcp:5080` e `adb reverse tcp:8081 tcp:8081` quando Platform Tools e depuração USB estiverem disponíveis. Configure `EXPO_PUBLIC_API_URL=http://127.0.0.1:5080` somente para esse cenário de desenvolvimento e inicie Expo com `npx expo start --localhost`. Confirmar compatibilidade do cliente instalado com o SDK do projeto antes de testar. Fora de desenvolvimento a API exige HTTPS.

Se optar por Wi-Fi, será necessária exposição local deliberada da API e regra de firewall limitada à rede privada; não abrir essas portas automaticamente nem expor Postgres/Redis. Não publicar túnel público como atalho.

Roteiro no Android:

1. Cadastrar entregador com dados de homologação autorizados, CPF válido e placa exclusiva; testar máscaras e erro de duplicidade.
2. Entrar com conta pendente; conferir acesso ao cadastro, sem ofertas.
3. Escolher PDF/JPEG/PNG, informar validade e enviar CNH; entrar novamente e enviar documento do veículo.
4. No portal administrativo, baixar e analisar documentos; aprovar documentos atuais e depois o cadastro.
5. Entrar no Android e confirmar situação aprovada; sair e entrar novamente.
6. Testar rede desligada, arquivo inválido, voltar do seletor sem escolher arquivo e teclado sem encobrir o botão.
7. Bloquear no portal e confirmar que o Android não consegue consultar/enviar dados com a sessão antiga.

Repetir em iPhone quando houver dispositivo e ambiente compatível. Não utilizar documentos pessoais reais desnecessariamente durante testes.

## Aceite automatizado isolado

`scripts/phase1-acceptance.sh` usa exclusivamente `coletas_phase1_acceptance` e porta 5081. Nunca apontar as fixtures para demonstração/produção. A remoção com `stop` descarta apenas esses dados fictícios. Manter WSL ativo durante os testes.

```powershell
dotnet publish src/Coletas.Api -c Release -o artifacts/registration-api
wsl -d coletas-dev -- bash scripts/phase1-acceptance.sh
Set-Location apps/web
$env:PLAYWRIGHT_PORT='5192'
$env:API_PROXY_TARGET='http://127.0.0.1:5081'
$env:COLETAS_REAL_ACCEPTANCE='1'
npx playwright test tests/phase1-real.spec.ts --project=desktop --workers=1 --output=../../artifacts/real-tests
```

Remova essas três variáveis do terminal antes de iniciar a demonstração. A fixture respeita a janela real de autenticação entre cenários; não reduz a proteção para ficar mais rápida. O CI padrão executa testes web simulados; este aceite exige ambiente isolado explicitamente preparado.
