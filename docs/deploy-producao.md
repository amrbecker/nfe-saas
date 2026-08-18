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

## 4. Cloudflare Pages — WebUI

1. Dashboard da Cloudflare → **Workers & Pages → Create → Pages → Connect to Git** → mesmo
   repositório.
2. Configuração de build:
   - **Build command:**
     ```bash
     curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir ./dotnet-sdk && export PATH="$PWD/dotnet-sdk:$PATH" && dotnet publish src/WebUI/NfeSaas.WebUI.csproj -c Release -o build_output
     ```
   - **Build output directory:** `build_output/wwwroot`
   - **Root directory:** `/` (raiz do repo)
3. Deploy. O `_redirects` (SPA fallback) e `_headers` (cache) já estão em
   `src/WebUI/wwwroot/` e são publicados automaticamente — nenhuma config extra na Cloudflare.
4. **Custom domains** → adicione `nfe.sideral.app.br`. Como o domínio já está na zona da
   Cloudflare, isso cria o registro DNS automaticamente (sem passo manual).
5. Confirme: abra `https://nfe.sideral.app.br` — deve carregar a tela de login e falar com
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
3. DSN da WebUI → edite `src/WebUI/wwwroot/appsettings.Production.json` no repo (o DSN de
   client é público por natureza — é assim que o Sentry funciona, não precisa de secret) e
   faça commit. Redeploy automático na Cloudflare Pages.

## 7. Resend — deixado pronto, sem ação necessária agora

`IEmailService`/`ResendEmailService` já existem e estão registrados no DI, mas nenhum código
chama isso ainda — nenhum email sai do sistema hoje. Quando decidir os fluxos (ex.: enviar
XML+DANFE ao destinatário, avisar trial acabando), é só:
1. Pegar a API key no dashboard do Resend e configurar `Resend__ApiKey`/`Resend__FromEmail`
   no Render (o `FromEmail` precisa ser de um domínio verificado no Resend — verifique
   `sideral.app.br` ou um subdomínio lá antes de usar).
2. Injetar `IEmailService` no handler correspondente e chamar `EnviarNFeAsync(...)`.

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
