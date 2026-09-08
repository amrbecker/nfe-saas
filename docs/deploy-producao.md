# Deploy em Produção — NfeSaas

Guia passo a passo para publicar o piloto usando as contas gratuitas já existentes.
Este é um runbook manual: cada serviço exige login OAuth/dashboard que só você tem acesso —
não é algo que dá pra automatizar por aqui.

## Arquitetura

```
                      ┌─────────────────────────┐
   Cliente (browser) →│  nfe.sideral.app.br      │  Cloudflare Pages
                      │  (Blazor WASM estático)  │  (grátis, CDN incluído)
                      └───────────┬──────────────┘
                                  │ HTTPS (fetch/JSON)
                                  ▼
                      ┌─────────────────────────┐
                      │ api.nfe.sideral.app.br   │  Render (free web service,
                      │ (ASP.NET Core, Docker)   │  Docker, docker/Dockerfile.api)
                      └───────────┬──────────────┘
                                  │ Npgsql (SSL)
                                  ▼
                      ┌─────────────────────────┐
                      │ Neon Postgres            │  (free tier, autosuspend)
                      └─────────────────────────┘

   Sentry   → monitoramento de erro (API + WebUI)
   Resend   → email transacional (integração pronta, nenhum disparo ainda)
   Cloudflare → DNS de sideral.app.br + CDN da Pages
```

**Por que subdomínio em vez de `sideral.app.br/nfe`:** path-based exigiria um Worker fazendo
proxy reverso na frente do site principal do Sideral, com risco de quebrar o que já está no ar.
Subdomínio é um registro DNS independente — zero interferência no site existente.

**Por que Render free funciona sem disco persistente:** as chaves de Data Protection (que
cifram `CertificadoSenha`/`CscToken`) foram migradas para caírem automaticamente na própria
Neon quando não há `DataProtection:KeysPath` configurado (ver
`src/Infrastructure/DependencyInjection.cs`). Os logs vão só pro console, que o Render já
captura como log da plataforma.

**Limitações do free tier a aceitar por enquanto (trocar quando houver mais clientes):**
- Render: dorme após ~15min sem tráfego → primeiro request depois disso demora 30-60s.
- Neon: compute em autosuspend após inatividade → primeira query depois disso soma mais
  alguns segundos. Se os dois estiverem "frios" ao mesmo tempo, o pior caso passa de 1 minuto.
- Sem múltiplas réplicas — não é um problema hoje (`Database__MigrateOnStartup=true` é seguro
  com 1 instância só).

---

## 1. Neon — banco de dados

1. Crie um projeto novo (região próxima ao Render — `us-east` costuma ser a mais próxima do
   Render `oregon`; se o Render ficar em outra região, ajuste para minimizar latência).
2. Crie o banco `nfesaas` (ou use o database default do projeto).
3. Pegue a connection string em **Connection Details** — formato Npgsql (não o `postgres://` URI
   que a Neon mostra por padrão). Monte assim:
   ```
   Host=<seu-endpoint>.neon.tech;Database=nfesaas;Username=<usuario>;Password=<senha>;SSL Mode=Require;Trust Server Certificate=true
   ```
   Guarde essa string — vai para `ConnectionStrings__DefaultConnection` no Render (passo 3).
4. **Backup / PITR (point-in-time recovery)** — confirme no dashboard Neon (**Settings → Backup**
   no projeto) a janela de retenção do plano atual. No free tier a Neon mantém PITR por um período
   curto (poucos dias) automaticamente, sem configuração adicional — mas **isso não foi testado**
   neste projeto e não deve ser assumido como suficiente sem verificar. Notas fiscais têm retenção
   legal obrigatória de 5 anos (`NotaFiscal.AnosRetencaoFiscal`) — perder o banco é perda de dado
   com obrigação legal de guarda, não só inconveniência operacional. Antes de aceitar clientes reais:
   - Confirme a janela de retenção exata do plano contratado.
   - Faça um teste de restore uma vez (branch de restore na Neon a partir de um ponto no passado)
     e documente que funcionou, com data.
   - Se o plano free não for suficiente, considere um `pg_dump` agendado (cron externo, ex. GitHub
     Actions com schedule) como camada adicional independente da plataforma.
   - Migrations rodam automaticamente no boot em produção (`Database__MigrateOnStartup=true`,
     `render.yaml`) — sem snapshot manual prévio. Antes de um deploy com migration destrutiva
     (`DropColumn`/`DropTable`), tire um snapshot manual (Neon → branch a partir do momento atual)
     como precaução extra.

## 2. Cloudflare — DNS

1. Se `sideral.app.br` ainda não estiver com nameservers apontando pra Cloudflare, faça isso
   primeiro (Cloudflare → Add a site → siga o wizard → troque os nameservers no painel do
   registro.br). Sem isso, nenhum dos passos de DNS abaixo funciona.
2. Não crie os registros de `nfe.sideral.app.br` e `api.nfe.sideral.app.br` ainda — isso é
   feito automaticamente quando você conecta o domínio customizado no Cloudflare Pages (passo 4)
   e no Render (passo 3). Adiantar aqui só cria registro solto sem destino.

## 3. Render — API

1. Dashboard → **New > Blueprint** → conecte o repositório GitHub `amrbecker/nfe-saas`.
2. O Render lê o `render.yaml` da raiz do repo e propõe o serviço `nfesaas-api`. Confirme.
3. Preencha os secrets pedidos (marcados `sync: false` no blueprint):

   | Variável | Valor |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | connection string da Neon (passo 1) |
   | `Jwt__Secret` | `openssl rand -base64 48` |
   | `Sentry__Dsn` | DSN do projeto Sentry da API (passo 6) — pode deixar vazio por enquanto e preencher depois |
   | `DataProtection__CertificateBase64` / `CertificatePassword` | opcional, ver passo 5 |
   | `Resend__ApiKey` / `FromEmail` | opcional, deixe vazio (nenhum email é enviado hoje) |

4. Deploy. Acompanhe o log — a primeira subida aplica as migrations automaticamente
   (`Database__MigrateOnStartup=true` já vem no blueprint) e não tem seed de demo (isso é só
   pro ambiente de dev local via `restart.sh`). Se quiser o escritório demo em produção pra
   testes, aplique `scripts/seed.sql` manualmente contra a Neon — não recomendado pro piloto
   real, prefira cadastrar o escritório de verdade pela tela de auto-cadastro.
5. Depois do primeiro deploy com sucesso: **Settings → Custom Domain** → adicione
   `api.nfe.sideral.app.br`. O Render mostra o registro CNAME a criar — como o domínio já está
   na Cloudflare, ele aparece automaticamente como opção de "Connect to Cloudflare" ou você cria
   o CNAME manualmente no painel da Cloudflare apontando pro host `*.onrender.com` que o Render
   indicar.
6. Confirme: `curl https://api.nfe.sideral.app.br/health` deve responder `Healthy`.

## 4. Cloudflare Workers & Pages — WebUI

A UI atual da Cloudflare unificou Pages em "Workers Builds" (baseado em Wrangler) — não existe
mais um campo de dashboard para "Build output directory". O diretório de publicação é definido
no `wrangler.jsonc` da raiz do repo (já versionado no projeto), na chave `assets.directory`.
Esse arquivo também define `not_found_handling: "single-page-application"`, que faz o papel do
antigo `_redirects` do Pages clássico (fallback de rota para o Blazor WASM). **Importante:** o
arquivo `_redirects` em `wwwroot/` (regra `/* /index.html 200`) precisou ser removido — ele ainda
é processado pelo Workers Builds e conflita com `not_found_handling`, causando erro de deploy
("Infinite loop detected... strip `.html` or `/index`", code 100324).

1. Dashboard da Cloudflare → **Workers & Pages → Create → Import a repository** (ou "Connect to
   Git") → mesmo repositório GitHub.
2. Configuração de build (só existem estes 4 campos, sem "output directory"):

   | Campo | Valor |
   |---|---|
   | **Build command** | ```curl -sSL https://dot.net/v1/dotnet-install.sh \| bash -s -- --channel 8.0 --install-dir ./dotnet-sdk && export PATH="$PWD/dotnet-sdk:$PATH" && dotnet publish src/WebUI/NfeSaas.WebUI.csproj -c Release -o build_output``` |
   | **Deploy command** | padrão (`npx wrangler deploy`) — lê o `wrangler.jsonc` automaticamente |
   | **Version command** | padrão |
   | **Root directory** | `/` (raiz do repo) |

   Em **Build watch paths**, o padrão (`Include paths: *`) serve — só controla o que dispara
   rebuild em monorepo, não afeta o deploy.
3. O `name` dentro de `wrangler.jsonc` precisa bater com o nome do projeto mostrado na lista do
   Workers & Pages — se você criar o projeto com outro nome, ajuste o `wrangler.jsonc` e faça
   commit antes de rodar o deploy.
4. Deploy. O `_headers` (cache) em `src/WebUI/wwwroot/` não deu erro de build (só o
   `_redirects` conflitava, ver acima), mas confira no DevTools (aba Network) se os headers de
   cache saem como esperado depois do primeiro deploy bem-sucedido — ainda não validamos isso
   fim a fim.
5. **Custom domains** → adicione `nfe.sideral.app.br`. Como o domínio já está na zona da
   Cloudflare, isso cria o registro DNS automaticamente (sem passo manual).
6. Confirme: abra `https://nfe.sideral.app.br` — deve carregar a tela de login e falar com
   `https://api.nfe.sideral.app.br` (já embutido em `wwwroot/appsettings.Production.json`,
   detectado automaticamente porque hosts estáticos sem o header `Blazor-Environment` assumem
   ambiente "Production" por padrão).

## 5. Cifrar as chaves de Data Protection (recomendado antes do primeiro cliente real)

Sem isso, quem tiver acesso de leitura à tabela `DataProtectionKeys` na Neon consegue decifrar
`CertificadoSenha`/`CscToken` de todas as empresas cadastradas. Gere um certificado
autoassinado uma única vez (guarde os arquivos `dp.key`/`dp.crt`/`dp.pfx` num cofre de senhas,
não no git):

```bash
openssl req -x509 -newkey rsa:2048 -keyout dp.key -out dp.crt -days 3650 -nodes -subj "/CN=NfeSaas-DP"
openssl pkcs12 -export -out dp.pfx -inkey dp.key -in dp.crt -passout pass:SUA_SENHA_FORTE
base64 -i dp.pfx | tr -d '\n'
```

Cole a saída em `DataProtection__CertificateBase64` no Render, e a senha escolhida em
`DataProtection__CertificatePassword`. Redeploy. **Perder este certificado depois que ele
começou a cifrar chaves reais = perder acesso aos certificados digitais e tokens CSC já
cadastrados** — mesmo cuidado que o volume `dp_keys` local.

## 6. Sentry — monitoramento

1. Crie dois projetos no Sentry: um `ASP.NET Core` (pra API) e um `Blazor WebAssembly` ou
   genérico `JavaScript` (pra WebUI — o SDK client-side não depende do tipo de projeto Sentry,
   só usa o DSN).
2. DSN da API → `Sentry__Dsn` no Render (passo 3).
3. DSN da WebUI → edite `src/WebUI/wwwroot/appsettings.Production.json` no repo e faça commit.
   Redeploy automático na Cloudflare Pages.

   O DSN de client é público por natureza — o Blazor WASM serve esse arquivo pro browser, então
   ele é legível por qualquer visitante; não é um secret. Ainda assim, **este repositório é
   público** e o arquivo fica versionado com o valor vazio de propósito: um DSN commitado aqui
   permite que terceiros injetem eventos falsos e queimem a cota do projeto. Preencha no seu
   fork/deploy, e rotacione o DSN no painel do Sentry se ele já tiver sido publicado.

## 7. Resend — necessário para o botão "Enviar por E-mail"

`IEmailService`/`ResendEmailService` já têm um call site: o botão "Enviar por E-mail" na tela de
detalhe da nota (`EnviarNFePorEmailCommandHandler`), acionado manualmente pelo usuário — não há
envio automático após autorização. Sem configurar isso, o botão sempre retorna erro
("Falha ao enviar e-mail").
1. Pegar a API key no dashboard do Resend e configurar `Resend__ApiKey`/`Resend__FromEmail`
   no Render (o `FromEmail` precisa ser de um domínio verificado no Resend — verifique
   `sideral.app.br` ou um subdomínio lá antes de usar).
2. Testar: autorizar uma NF-e em homologação, abrir a nota na WebUI, clicar "Enviar por E-mail".

## 8. Alertas no Sentry (não é código — configurar no dashboard)

Sentry captura exceções e 10% das transações (`TracesSampleRate = 0.1` em `Program.cs`), mas isso
sozinho não avisa ninguém — sem alerta configurado, um erro em produção só é descoberto se alguém
for olhar o dashboard manualmente. Configure em **Sentry → Alerts** (por projeto, API e WebUI):
- Taxa de erro acima de um limite (ex.: > 5% das transações num período de 5-10 min).
- Um erro novo (nunca visto antes) — sinaliza regressão logo após deploy.
- Latência p95 acima de ~1s (usa a amostra de 10% do tracing).
Aponte os alertas para um canal que alguém de fato monitore (e-mail, Slack). Documente aqui a data
em que os alertas foram configurados, para não ficar assumido silenciosamente que "alguém já fez isso".

## 9. Rollback — procedimento

**Rollback de deploy (código):** tanto Render quanto Cloudflare Pages guardam o histórico de builds
e permitem reverter para um deploy anterior pelo próprio dashboard (Render → Deploys → "..." →
Rollback; Cloudflare Pages → Deployments → "..." → Rollback to this deployment). Não depende de
git revert nem de reconstruir nada — é a via mais rápida para reverter um deploy ruim.

**Rollback de migration (banco):** mais delicado, porque `Database__MigrateOnStartup=true` aplica
migrations automaticamente no boot — revertê-las não é automático.
1. Gere o script de reversão localmente: `dotnet ef migrations script <MigrationAnterior> <MigrationAtual> --project src/Infrastructure --startup-project src/API` (a ordem inverte o `Up`, usando o `Down()` de cada migration — confirme que o `Down()` existe e faz sentido antes de aplicar).
2. Rode esse script direto contra o Neon (via `psql` com a connection string de produção) **antes**
   de fazer o rollback do deploy de código — código antigo não espera colunas/tabelas novas.
3. Se a migration já rodou e causou perda de dados (ex.: um `DropColumn` que não devia ter rodado),
   pare aqui e recorra ao backup/PITR (seção 1, item 4) em vez de tentar reconstruir manualmente.

Nenhum destes dois procedimentos foi testado em produção neste projeto até o momento — antes de
depender deles num incidente real, execute um simulacro controlado (ex.: em homologação) pelo menos
uma vez.

## Checklist antes de dar acesso ao escritório piloto

- [ ] `https://api.nfe.sideral.app.br/health` responde `Healthy`
- [ ] `https://nfe.sideral.app.br` carrega e loga
- [ ] Cadastro do escritório real + upload do certificado A1 funcionando (ambiente SEFAZ
      **Produção** — sem stub, transmissão real)
- [ ] Certificado de Data Protection configurado (passo 5) — antes de cadastrar o certificado
      A1 real do cliente
- [ ] Sentry recebendo eventos de teste (force um erro deliberado e confirme que aparece no
      dashboard)
- [ ] Primeira emissão de teste em Produção acompanhada manualmente, e o primeiro
      cancelamento cruzado com a consulta pública do portal da SEFAZ (ver ressalva na revisão
      de produção: não há como testar contra o webservice real sem um cliente de verdade)
- [ ] Janela de PITR do Neon confirmada e teste de restore feito pelo menos uma vez (seção 1, item 4)
- [ ] Alertas do Sentry configurados (taxa de erro, erro novo, latência) — seção 8
- [ ] Rollback de deploy testado uma vez em homologação (seção 9)
