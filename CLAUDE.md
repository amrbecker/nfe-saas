# NfeSaas — Instruções para Claude Code

## Visão Geral

SaaS multi-tenant para emissão de NF-e e NFC-e, usado por escritórios de contabilidade.
Hierarquia: **Escritório → Empresa → NotaFiscal**. Usuários pertencem ao Escritório, não à Empresa.

Cada Escritório recebe **trial automático de 30 dias** ao se cadastrar; depois disso o login é bloqueado até ativação de plano pago (ver [Trial e Plano](#trial-e-plano-do-escritório)).

## Stack

| Camada | Tecnologia |
|--------|-----------|
| API | ASP.NET Core 8 + MediatR + EF Core 8 |
| Frontend | Blazor WASM + MudBlazor 6.20.0 |
| Banco | PostgreSQL 16 |
| Auth | JWT com dois tokens (veja fluxo abaixo) |
| Cifragem em repouso | ASP.NET Data Protection (volume `dp_keys`) |
| Infra | Docker Compose |
| Testes | xUnit + SpecFlow (BDD) + Testcontainers |

## Estrutura de Projetos

```
src/
  Domain/          # Entidades, Enums, Interfaces (sem dependências externas)
  Application/     # Commands, Queries, DTOs, Behaviors (MediatR)
  Infrastructure/  # EF Core, Repositórios, Serviços externos (SEFAZ, XML)
  API/             # Controllers, Middleware, Program.cs
  WebUI/           # Blazor WASM (Services, Pages, Components)
tests/
  NfeSaas.Tests.Unit/        # Testes de domínio e serviços isolados
  NfeSaas.Tests.Integration/ # Testes com banco real (Testcontainers)
  NfeSaas.Tests.BDD/         # Cenários Gherkin (SpecFlow)
```

## Fluxo JWT (dois tokens)

1. `POST /api/auth/login` → retorna `accessToken` sem `empresa_id` + lista de Empresas + `assinatura` (status de trial/plano)
2. `POST /api/auth/selecionar-empresa` → retorna novo `accessToken` com `empresa_id` e `escritorio_id`
3. Todas as rotas protegidas exigem o token com `empresa_id`

**Códigos HTTP do login:**
- `200` — sucesso (`LoginResultDto` com `assinatura`)
- `401` — credenciais inválidas (`codigo: "CredenciaisInvalidas"`)
- `402` — trial expirado, ative plano (`codigo: "TrialExpirado"`, `assinatura` preenchida)
- `403` — escritório suspenso (`codigo: "EscritorioSuspenso"`)

Toda resposta de falha inclui `{ message, codigo, assinatura? }` — a UI pode tratar cada caso (`Login.razor` usa `Severity.Warning` para trial/suspenso e `Severity.Error` para credencial).

## Trial e Plano do Escritório

- `Escritorio.Criar(...)` define `TrialInicioEm = UtcNow` e `TrialFimEm = UtcNow + 30 dias` (constante `Escritorio.DiasTrialPadrao`).
- `Escritorio.CalcularStatusAssinatura()` retorna `StatusAssinaturaEscritorio` (`TrialAtivo | Pago | TrialExpirado | Suspenso`). Suspenso domina; Pago prevalece sobre TrialAtivo quando `PlanoAtivoAteEm > UtcNow`.
- `Escritorio.AtivarPlanoPago(ativoAte, momentoPagamento?)` — exige data futura, lança `InvalidOperationException` caso contrário.
- `Escritorio.PodeAcessar()` é o gate único usado por `LoginCommandHandler` e `SelecionarEmpresaCommandHandler`.
- **Não existe plano gratuito** — `PlanoSaas` tem `Basico = 1`, `Profissional = 2`, `Enterprise = 3`. Auto-cadastro exige escolha explícita.
- Endpoint `POST /api/escritorio/ativar-plano [Admin]` com `AtivarPlanoPagoDto { AtivoAteUtc, ValorPago? }` — em produção deve ser substituído por webhook do gateway de pagamento.

## Cadastrar o Próprio Escritório como Empresa Emitente

O escritório (PJ com CNPJ) pode emitir NF-e em nome próprio sem precisar criar uma "empresa cliente" duplicando dados:

- Endpoint `POST /api/escritorio/cadastrar-como-empresa [Admin]` com `CadastrarEscritorioComoEmpresaDto`
- Página `/escritorio-como-empresa` (`src/WebUI/Pages/EscritorioComoEmpresa.razor`)
- **Idempotência:** se já existe Empresa com o CNPJ do escritório no mesmo Escritório, retorna a existente sem recriar
- **Bloqueio:** se o CNPJ existe em outro Escritório, retorna null (não permite "roubar" o CNPJ)
- Copia `RazaoSocial`, `NomeFantasia`, `Cnpj`, `Email`, `Telefone` do escritório; recebe apenas endereço + dados fiscais (IE, CEP, CodigoMunicipio, RegimeTributario, AmbienteSefaz, CNAE opcional)

## Personalização da UI por Perfil

`PersonalizacaoService` carrega a `ConfiguracaoEmpresa` da empresa selecionada e expõe `PerfilSimplificado` com flags semânticas usadas pela UI (`MostrarNFCe`, `MostrarTributacaoAvancada`, `MostrarCadastroProdutos`, etc.). O cache invalida automaticamente ao trocar de empresa.

- Quando criar novas páginas/componentes, prefira consumir flags semânticas em vez de testar enums diretamente
- Flags ainda não conectadas (`MostrarContingencia`, `MostrarRelatoriosAvancados`) estão prontas para usar quando as páginas existirem

## Convenções de Código

- **Domínio em português**: nomes de entidades, propriedades, DTOs e labels de UI seguem PT-BR (Escritório, Empresa, NotaFiscal, Destinatario, etc.)
- **Infraestrutura em inglês**: namespaces, padrões técnicos, nomes de controllers usam convenções inglesas
- Entidades de domínio têm construtores `protected` + factory method estático (`Criar(...)`)
- Propriedades de entidades são `private set` — mutação ocorre apenas via métodos da entidade
- Commands e Queries usam `record` com MediatR
- Repositórios sempre recebem `CancellationToken ct = default`
- **Usar `DateTime.UtcNow`** no Domain, Application e Infrastructure (Controllers inclusive). `DateTime.Now` permitido apenas em UI Blazor onde faz sentido o fuso local do usuário
- **Formatação decimal em XML/SEFAZ deve usar `InvariantCulture`** — o `XmlNFeService` tem helpers `F2(decimal)`/`F4(decimal)` para isso. Nunca `{decimal:F2}` direto em string interpolada (em pt-BR gera vírgula → SEFAZ rejeita pelo XSD)

## Banco de Dados e Migrações

O container da API **não tem o SDK do EF Core**. Para aplicar migrações:

```bash
# 1. Gerar script SQL idempotente (rodar na máquina host)
dotnet ef migrations script --idempotent -o migration.sql \
  --project src/Infrastructure --startup-project src/API

# 2. Copiar para o container do postgres e aplicar
docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/migration.sql
```

Nunca rodar `dotnet ef database update` diretamente nos containers. O `restart.sh` já automatiza esse fluxo.

**Design-time factory:** `NfeDbContextDesignFactory` constrói o `DbContext` sem `IDataProtectionProvider` para o tooling do EF (`dotnet ef`). No runtime, o construtor de 2 parâmetros é selecionado e a cifragem dos secrets fica ativa.

## Cifragem em Repouso (Secrets da Empresa)

- `Empresa.CertificadoSenha` e `Empresa.CscToken` são cifrados via `EncryptedStringConverter` (ASP.NET Data Protection) com prefixo `enc:v1:`
- Valores legados em texto claro (sem prefixo) continuam sendo lidos como estão — re-cifragem acontece no próximo `UPDATE`
- Chaves de cifragem ficam no volume Docker `dp_keys` (`/app/dpkeys`), configurado via `DataProtection__KeysPath`. **Perder esse volume = perder acesso aos secrets já gravados.**
- Sem `DataProtection__KeysPath` configurado (hosts sem disco persistente, ex.: Render free tier), cai automaticamente no fallback `PersistKeysToDbContext<NfeDbContext>()` — chaves na tabela `DataProtectionKeys` da própria base Postgres (ver `DependencyInjection.AddInfrastructure`). Configure `DataProtection__CertificateBase64`/`CertificatePassword` para cifrar essas chaves em repouso (`ProtectKeysWithCertificate`) — sem isso, quem lê a tabela decifra os secrets de todas as empresas.
- Para evoluir para Azure Key Vault / AWS KMS, basta trocar `AddDataProtection()` em `DependencyInjection.cs` — o converter é agnóstico ao backend

## Executar os Serviços

**Atalho:**
```bash
./restart.sh                                       # padrão: build + migrations + seed (se vazio) + abre IDE
./restart.sh --clean                                # apaga volumes (banco + dp_keys) e recomeça
./restart.sh --no-build --skip-migrations           # restart rápido
./restart.sh --no-ide --no-seed                     # CI / iteração
```

O script valida `.env`, sobe os containers, aplica migrations idempotentes, aplica seed se o banco estiver vazio, aguarda `/health` e abre a solution.

**Manual:**
```bash
docker compose up -d --build               # subir tudo
docker compose logs -f api                 # ver logs da API
docker compose up -d postgres              # apenas o banco (útil para dev local)
```

Endpoints locais:
- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- WebUI: http://localhost:5002
- Health: http://localhost:5001/health

## Configuração (.env)

`.env` é obrigatório e não vai pro git. Copie do template e preencha:

```bash
cp .env.example .env
# Gere secrets fortes:
#   JWT_SECRET:        openssl rand -base64 48       (mín. 32 chars; fail-fast no startup)
#   POSTGRES_PASSWORD: openssl rand -base64 24
```

O `Program.cs` valida `Jwt:Secret` (>= 32 chars, sem placeholders `SUA_CHAVE`/`__TROCAR`) e `ConnectionStrings:DefaultConnection` (não vazio) no boot.

## Executar Testes

```bash
# Unitários
dotnet test tests/NfeSaas.Tests.Unit

# Integração (precisa do Docker para Testcontainers)
dotnet test tests/NfeSaas.Tests.Integration

# BDD (SpecFlow)
dotnet test tests/NfeSaas.Tests.BDD

# Todos
dotnet test
```

Os testes de integração e BDD sobem o banco via Testcontainers — não precisam do Docker Compose rodando. As fixtures (`DatabaseFixture`, `TestWebApplication`) já injetam `Jwt:Secret` e `ConnectionStrings:DefaultConnection` via `UseSetting` para a validação do `Program.cs` passar.

## Dados de Seed (Demo)

| Recurso | Valor |
|---------|-------|
| Escritório ID | `cccccccc-cccc-cccc-cccc-cccccccccccc` |
| Escritório CNPJ | `99.999.999/0001-91` |
| Empresa ID | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| Empresa CNPJ | `00.000.000/0001-91` |
| Usuário admin | `admin@nfesaas.com.br` / `Admin@123` |

Seed atribui ao Escritório demo `PlanoAtivoAteEm = NOW() + 1 ano` para não bloquear demonstrações com trial expirado.

## Variáveis de Ambiente Importantes

| Variável | Obrigatória | Descrição |
|----------|-------------|-----------|
| `ConnectionStrings__DefaultConnection` | sim | Connection string PostgreSQL |
| `Jwt__Secret` | sim | Segredo JWT (mín. 32 chars, sem placeholder) |
| `Jwt__Issuer` | não (default `NfeSaas`) | Issuer do token |
| `Jwt__Audience` | não (default `NfeSaas.WebUI`) | Audience do token |
| `WebUI__BaseUrl` | não (default `http://localhost:5002`) | URL da WebUI para CORS |
| `DataProtection__KeysPath` | recomendada | Path para persistir chaves de cifragem entre restarts |
| `Ncm__UpdateSourceUrl` | não | URL HTTPS para atualização semanal da tabela NCM |
| `Ncm__UpdateOnStartup` | não (default `false`) | Atualiza NCM no boot |

## Isolamento Multi-Tenant

- Todo acesso a dados deve filtrar por `EmpresaId` (obtido do claim JWT)
- Repositórios **nunca** retornam dados de outras empresas
- Ao criar novos endpoints ou queries, sempre verificar se o `EmpresaId` do token bate com o recurso solicitado

## Serviços Externos

- **ISefazService**: stub em dev/homologação (`Sefaz:UseRealWebservice=false`, default), transmissão real em produção — autorização, cancelamento, CC-e, inutilização, manifestação e consulta de protocolo todos via webservice de verdade (RecepcaoEvento4/NFeInutilizacao4/NFeConsultaProtocolo4). URLs por UF e mapeamento de contingência SVC-AN/SVC-RS em `SefazService.cs` — verificados contra fontes oficiais em 2026-08-18, revalidar periodicamente. Sempre usar `AmbienteSefaz.Homologacao` nos testes
- **IXmlNFeService**: geração e assinatura XML da NF-e (exige certificado digital na Empresa). Use os helpers `F2`/`F4` para qualquer formatação decimal no XML. Cancelamento/CC-e/Manifestação usam o formato de evento (`<envEvento>`, tpEvento 110111/110110/2102xx) — não o `<cancNFe>` standalone pré-2013
- **IImpostoCalculoService**: cálculo de ICMS, ICMS-ST, PIS e COFINS — testado via testes unitários isolados
- **ICertificadoService**: upload/validação de PFX A1. Rota de upload limitada a 256 KB e exige role `Admin`
- **IEmailService** (`ResendEmailService`): envia XML+DANFE ao destinatário via `EnviarNFePorEmailCommandHandler`, acionado pelo botão "Enviar por E-mail" na tela de detalhe da nota (`NotaDetalhe.razor`) — envio manual, não automático após autorização. Retorna `bool` (sucesso/falha) para o handler não reportar sucesso falso quando `Resend__ApiKey`/`FromEmail` não estiverem configurados no Render

## Produção

Deploy: Render (API, Docker) + Cloudflare Pages (WebUI estática) + Neon (Postgres) + Cloudflare
(DNS de `sideral.app.br`) + Sentry (monitoramento). Passo a passo completo em
[`docs/deploy-producao.md`](docs/deploy-producao.md); config do serviço Render como código em
`render.yaml`. Free tier em todos — Render dorme após inatividade (cold start no próximo
request), Neon entra em autosuspend; aceitável para o piloto, trocar quando houver mais clientes.

## Padrões a Seguir

- Novos casos de uso → novo Command/Query em `Application/`, handler correspondente, interface no repositório se necessário
- Novos campos no banco → nova migration EF (`dotnet ef migrations add NomeMigration`), depois `restart.sh` aplica
- Não adicionar lógica de negócio nos Controllers — apenas despachar para MediatR
- Não usar `DateTime.Now` no Domain/Application/Infrastructure — usar `DateTime.UtcNow`
- Nunca interpolar `{decimal:F2}`/`{decimal:F4}` em XML SEFAZ — usar `F2(...)`/`F4(...)` (InvariantCulture)
- Senhas e tokens sensíveis (`CertificadoSenha`, `CscToken`) vão por `EncryptedStringConverter` automaticamente — não escrever em claro no banco
- Antes de criar uma página/menu novo na WebUI, verificar se vale gatear via `PersonalizacaoService` (modo simples não deve ver complexidade desnecessária)
