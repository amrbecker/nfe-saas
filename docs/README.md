# 🧾 NfeSaas — SaaS Multi-Tenant de Emissão de NF-e e NFC-e

Plataforma completa para emissão, gestão e arquivamento de **Nota Fiscal Eletrônica (NF-e modelo 55)** e **Nota Fiscal de Consumidor Eletrônica (NFC-e modelo 65)**, projetada para **escritórios de contabilidade** que precisam emitir documentos fiscais em nome de **múltiplas empresas clientes** sob uma única operação — com isolamento estrito de dados, conformidade fiscal, contingência SEFAZ e arquitetura moderna.

> **Quem usa:** contadores, BPOs fiscais, ERPs SaaS e empresas que precisam de uma camada fiscal embarcável.
> **Para o que serve:** substituir emissores monolíticos, planilhas e integrações ponto-a-ponto com a SEFAZ por uma plataforma única, multi-empresa, com auditoria e conformidade fim a fim.

---

## 📑 Sumário

1. [Diferenciais Competitivos](#-diferenciais-competitivos)
2. [Problemas que o Produto Resolve](#-problemas-que-o-produto-resolve)
3. [Funcionalidades](#-funcionalidades)
4. [Segurança e Proteção de Dados](#-segurança-e-proteção-de-dados)
5. [Arquitetura](#%EF%B8%8F-arquitetura)
6. [Início Rápido — Docker Compose](#-início-rápido--docker-compose)
7. [Desenvolvimento Local](#%EF%B8%8F-desenvolvimento-local)
8. [Modelo Multi-Tenant e Fluxo JWT](#-modelo-multi-tenant-e-fluxo-jwt)
9. [Conformidade Fiscal e Imutabilidade](#-conformidade-fiscal-e-imutabilidade)
10. [Endpoints da API](#-endpoints-da-api)
11. [Impostos e Regimes Tributários Suportados](#-impostos-e-regimes-tributários-suportados)
12. [Eventos Fiscais Suportados](#-eventos-fiscais-suportados)
13. [Contingência SEFAZ](#%EF%B8%8F-contingência-sefaz)
14. [Tecnologias](#%EF%B8%8F-tecnologias)
15. [Variáveis de Ambiente](#%EF%B8%8F-variáveis-de-ambiente)
16. [Testes](#-testes)
17. [Publicação em Produção](#-publicação-em-produção)

---

## 🏆 Diferenciais Competitivos

| # | Diferencial | O que entrega |
|---|-------------|---------------|
| 1 | **Multi-tenant nativo de 3 níveis** | Hierarquia **Escritório → Empresa → NotaFiscal** modelada no domínio e reforçada por **dois claims JWT** (`escritorio_id` + `empresa_id`). Trocar entre empresas clientes não exige novo login. |
| 2 | **Isolamento fiscal rígido** | Nenhuma query retorna dados de outra empresa. O `EmpresaId` do token é a chave de leitura em todos os repositórios. Auditoria de cada operação por usuário, empresa e IP. |
| 3 | **Imutabilidade fiscal automática** | Interceptor de EF Core (`FiscalImmutabilityInterceptor`) impede alteração ou exclusão de documentos autorizados/cancelados. **Retenção legal de 5 anos** (Lei 10.522/02 + CTN art. 173) bloqueada no domínio. |
| 4 | **Stack 100% .NET 8** | Geração e assinatura XML com criptografia nativa do .NET (RSA X509) — **sem dependência de Java, NodeJS ou DLLs externas**. Único runtime, único processo, deploy mínimo. |
| 5 | **Cobertura tributária completa** | ICMS (CST + CSOSN), **ICMS-ST com MVA**, **DIFAL automático** para consumidor final interestadual, **FCP**, **IPI**, **PIS/COFINS**, base reduzida, diferimento, isenção, alíquota zero. |
| 6 | **Suporte aos dois regimes** | Simples Nacional (CSOSN) e Regime Normal/Lucro Real/Presumido (CST) — validação cruzada CRT × CST/CSOSN antes do envio. |
| 7 | **Eventos fiscais nativos** | Cancelamento, **Carta de Correção Eletrônica (CC-e até 20 sequenciais)**, **Inutilização de numeração** e **Manifestação do destinatário** (Confirmação, Ciência, Desconhecimento, Não-realizada). |
| 8 | **Contingência SEFAZ embarcada** | Suporte a **SVC-AN**, **SVC-RS** e **FS-DA** quando a SEFAZ autorizadora estiver fora do ar. |
| 9 | **Validação XSD oficial** | Os schemas v4.00 da NF-e são carregados em memória e usados para validar **antes do envio à SEFAZ** — reduz drasticamente o número de rejeições. |
| 10 | **DANFE e DANFE NFC-e com QR Code** | Geração nativa de PDF via **QuestPDF** (licença Community), incluindo hash CSC para a NFC-e. |
| 11 | **Certificado A1 por empresa** | Cada empresa cliente tem seu próprio certificado armazenado, validado e expira-aware — uma instalação serve dezenas de CNPJs. |
| 12 | **Auto-cadastro com trial de 30 dias** | Onboarding sem cartão: o contador cria o próprio tenant na WebUI, escolhe o plano e começa a emitir. Após 30 dias, o login retorna **HTTP 402 com `codigo: TrialExpirado`** e a UI orienta a ativação — bloqueio gracioso, sem deletar dados. |
| 13 | **Wizard de personalização → UI adaptativa** | Configuração inicial por empresa (perfil de cliente, tipo de produto, volume, nível de automação) gera **flags semânticas** consumidas pela WebUI: modo simples esconde NFC-e, IPI/FCP/DIFAL, inutilizações e cadastros — sem opção desnecessária para quem não precisa. |
| 14 | **Escritório como emissor próprio** | O escritório (PJ com CNPJ) pode emitir NF-e em nome próprio com um clique — endpoint **idempotente por CNPJ** evita duplicar dados, e copia razão social/contato do tenant automaticamente. |
| 15 | **Open-source self-hosted** | Stack 100% containerizada (Docker Compose). **Sem custo por nota emitida**, sem amarração a fornecedor SaaS terceiro, dados ficam na infraestrutura do cliente. |
| 16 | **Arquitetura limpa e testável** | DDD + CQRS (MediatR) + Clean Architecture + EF Core 8. Testes Unit, Integration (Testcontainers) e BDD (SpecFlow) prontos — **536 testes passando**. |
| 17 | **Mensagens 100% em PT-BR** | Domínio, UI e mensagens de erro em português brasileiro, alinhadas ao vocabulário fiscal nacional. |
| 18 | **Segurança em camadas** | Cifragem em repouso de senhas de certificado A1 e tokens CSC (ASP.NET Data Protection), RNG criptográfico na chave de acesso da NF-e, escape de XML contra injection, **InvariantCulture forçado** em decimais do XML (vírgula = rejeição SEFAZ), JWT secret com fail-fast no startup, upload de certificado restrito a admin com limite de tamanho — veja [Segurança e Proteção de Dados](#-segurança-e-proteção-de-dados). |

---

## 🎯 Problemas que o Produto Resolve

### Para o escritório de contabilidade
- **Múltiplas empresas, um login:** o contador autentica uma vez e navega entre todos os CNPJs que atende sem precisar logar/sair. O seletor de empresa fica sempre visível no topo da UI.
- **Onboarding em minutos:** auto-cadastro do escritório → criação da primeira empresa cliente → upload do certificado A1 → emissão da primeira nota — sem ticket de suporte.
- **Gestão centralizada de usuários:** o admin do escritório cria contadores assistentes (`Admin` ou `User`), ativa/desativa, redefine senha e revoga acesso a qualquer momento.
- **Visibilidade financeira por cliente:** dashboard com faturamento do mês, contagem de notas autorizadas/canceladas/pendentes e curva de faturamento diário **por empresa**.

### Para conformidade fiscal
- **Imutabilidade após autorização:** uma vez autorizada pela SEFAZ, a nota é congelada. O domínio bloqueia mutação e o interceptor de EF Core impede UPDATE/DELETE indevidos — sem janela para erro humano.
- **Retenção legal de 5 anos:** documentos autorizados/cancelados não podem ser excluídos antes do prazo legal (Lei 10.522/02 e CTN art. 173). O domínio expõe `DataDescarteAutorizado` para auditoria.
- **Trilha de auditoria completa:** `AuditService` registra `Empresa`, `Usuário`, `Ação` (autorização, cancelamento, CC-e, etc.), `Chave NF-e`, `IP de origem` e timestamp.
- **Validação antes do envio:** CNPJ, CPF, CFOP (compatível com saída/entrada e intra/interestadual), NCM (8 dígitos), UF, CEP, código IBGE, faixa CST × CSOSN × CRT — tudo verificado **antes** de gastar uma chamada à SEFAZ.

### Para a operação fiscal complexa
- **Operações interestaduais com DIFAL automático:** quando há diferença entre UF emissor e UF destinatário **e** o destinatário é não-contribuinte, o DIFAL é calculado e gravado sem intervenção do usuário.
- **ICMS-ST com MVA:** alíquotas interna e interestadual configuráveis por item, base de cálculo por margem de valor agregado.
- **NFC-e com QR Code:** geração do hash CSC e do QR Code de consulta no DANFE — cumprindo o layout obrigatório para varejo.
- **Numeração à prova de race:** índice único `(EmpresaId, Tipo, Série, Número, Ambiente)` + transação por emissão. Duas requisições simultâneas nunca geram a mesma numeração.

### Para indisponibilidade da SEFAZ
- **Contingência embarcada:** quando a SEFAZ autorizadora está fora, o sistema permite emitir em **SVC-AN**, **SVC-RS** ou **FS-DA**, mantendo a operação do varejo no ar.
- **Inutilização legal de numeração:** lacunas de numeração causadas por falhas podem ser inutilizadas via evento SEFAZ próprio, mantendo a sequência fiscal íntegra.

### Para o ciclo de vida do documento
- **Cancelamento dentro de 24h:** com assinatura digital e justificativa.
- **Carta de Correção Eletrônica (CC-e):** até **20 sequenciais** por chave de acesso, conforme manual SEFAZ.
- **Manifestação do destinatário:** Confirmação, Ciência, Desconhecimento e Operação Não Realizada — fechando o ciclo do compliance.
- **Download XML e DANFE:** a qualquer momento, para envio ao contador, ao destinatário ou arquivamento.

---

## ⚙️ Funcionalidades

### Gestão multi-empresa
- ✅ Auto-cadastro de Escritório (CNPJ, e-mail, telefone, plano obrigatório — sem plano Free)
- ✅ Cadastro ilimitado de Empresas (CNPJs clientes) por Escritório
- ✅ **Cadastrar o próprio escritório como empresa emitente** com um clique (idempotente por CNPJ)
- ✅ Gestão de Usuários por Escritório (roles `Admin` e `User`, ativar/desativar/excluir)
- ✅ Seletor de empresa no header (troca de contexto sem novo login)
- ✅ Wizard de Configuração Inicial por empresa (perfil cliente, volume, automação)
- ✅ Cadastro completo da Empresa (Razão Social, CNPJ, IE, IM, CNAE, endereço, regime tributário, ambiente SEFAZ)

### Trial e ativação de plano (`StatusAssinaturaEscritorio`)
- ✅ Trial automático de **30 dias** ao auto-cadastrar (constante `Escritorio.DiasTrialPadrao`)
- ✅ Planos `Basico` / `Profissional` / `Enterprise` — escolha obrigatória no cadastro
- ✅ `LoginCommandHandler` retorna `LoginCommandResult` discriminado (Sucesso × Falha)
- ✅ Códigos HTTP no login: **200** sucesso, **401** credencial inválida, **402** trial expirado, **403** escritório suspenso
- ✅ `AssinaturaDto` no `LoginResultDto` traz Plano, Status, DiasRestantesTrial, TrialFimEm, PlanoAtivoAteEm
- ✅ `Login.razor` exibe badge de trial e mensagens contextuais por código de falha
- ✅ Endpoint `POST /api/escritorio/ativar-plano [Admin]` (em produção: webhook do gateway de pagamento)
- ✅ `SelecionarEmpresaCommandHandler` valida `escritorio.PodeAcessar()` em todas as trocas de empresa
- ✅ Suspensão administrativa (`Escritorio.Suspender()`) domina sobre Pago/TrialAtivo

### Personalização adaptativa da UI (`PersonalizacaoService`)
- ✅ Flags semânticas derivadas do wizard: `ModoSimplificado`, `MostrarNFCe`, `MostrarTributacaoAvancada`, `MostrarCadastroProdutos`, `MostrarCadastroClientes`, `MostrarInutilizacoes`, `MostrarContingencia`, `MostrarRelatoriosAvancados`, `ExplicacoesDetalhadas`
- ✅ `EmitirNFe.razor` esconde IPI/FCP/DIFAL quando `MostrarTributacaoAvancada = false`
- ✅ `MainLayout.razor` esconde menus (Produtos, Clientes, Inutilizações) conforme perfil
- ✅ Cache invalidado automaticamente ao trocar de empresa

### Cadastros operacionais
- ✅ **Produtos:** Código, NCM, CEST, CFOP padrão, unidade, origem da mercadoria, EAN, código ANP, valor unitário padrão, ativo/inativo
- ✅ **Clientes (destinatários):** Pessoa Física, Pessoa Jurídica ou Estrangeiro; CPF/CNPJ; endereço; indicador de IE (Contribuinte, Isento, Não-Contribuinte)
- ✅ Ativação/desativação lógica preservando histórico

### Certificado Digital
- ✅ Upload de certificado A1 (`.pfx` ou `.p12`) por empresa
- ✅ Validação imediata: senha, formato, expiração, CNPJ titular
- ✅ Exibição de validade e CNPJ no painel
- ✅ Bloqueio de emissão quando certificado expira ou está ausente

### Emissão fiscal
- ✅ **NF-e (modelo 55)** — operação B2B
- ✅ **NFC-e (modelo 65)** — varejo / consumidor final com QR Code
- ✅ Finalidades: **Normal, Complementar, Ajuste, Devolução**
- ✅ Operações: **Entrada / Saída**
- ✅ Múltiplos itens por nota (com cálculo de impostos por item)
- ✅ Destinatário PF, PJ ou Estrangeiro
- ✅ Transporte com 6 modalidades de frete (CIF, FOB, terceiros, próprio remetente, próprio destinatário, sem frete)
- ✅ Pagamento (forma e valor)
- ✅ Informações adicionais

### Ciclo de vida da nota
- ✅ **Rascunho → Enviada → Autorizada / Rejeitada / Denegada**
- ✅ **Cancelamento** (assinado, com justificativa) — janela de 24h
- ✅ **Carta de Correção Eletrônica (CC-e)** — até 20 sequenciais por chave
- ✅ **Inutilização de numeração** (range de números por série/ano)
- ✅ **Manifestação do destinatário** (Confirmação, Ciência, Desconhecimento, Não-Realizada)
- ✅ **Retenção fiscal de 5 anos** com bloqueio de exclusão

### Impostos
- ✅ **ICMS** (CST 00–90) — tributada, base reduzida, diferimento, isenta, suspensão, ST
- ✅ **ICMS Simples Nacional** (CSOSN 101–900) — com/sem permissão de crédito, ST, imune
- ✅ **ICMS-ST** com MVA, alíquota interna e alíquota interestadual
- ✅ **DIFAL** automático em operações interestaduais para consumidor final
- ✅ **FCP** (Fundo de Combate à Pobreza)
- ✅ **PIS** e **COFINS** (CST 01–99)
- ✅ **IPI** (opcional por item)
- ✅ Base de cálculo com redução percentual
- ✅ Cálculo centralizado em `IImpostoCalculoService` (auditável e testável)

### Geração e assinatura XML
- ✅ Layout **NF-e v4.00** (padrão SEFAZ atual)
- ✅ Assinatura digital RSA X509 nativa do .NET
- ✅ Validação contra **XSD oficial SEFAZ** antes do envio (schemas em `Infrastructure/Schemas`)
- ✅ Assinatura específica para eventos (CC-e, Manifestação) e Inutilização
- ✅ Armazenamento dos XMLs de envio, retorno e cancelamento

### Integração SEFAZ
- ✅ Envio síncrono (NfeAutorizacao4)
- ✅ Cancelamento (RecepcaoEvento)
- ✅ Consulta de chave de acesso
- ✅ Status do serviço (StatusServico)
- ✅ Eventos: CC-e, Inutilização, Manifestação
- ✅ Comunicação SOAP/HTTP com **mTLS** (certificado A1 cliente)
- ✅ Stub de desenvolvimento (`AmbienteSefaz.Homologacao`) para testes sem SEFAZ real

### DANFE / PDF
- ✅ DANFE **NF-e** padrão A4 (header empresa, destinatário, itens, totais, transporte, pagamento, informações adicionais)
- ✅ DANFE **NFC-e** com **QR Code de consulta** e hash CSC
- ✅ Geração com **QuestPDF** (sem dependência de impressora ou serviço externo)
- ✅ Download via `/api/notas-fiscais/{id}/danfe`

### Dashboard e relatórios
- ✅ KPIs do mês: Faturamento, Autorizadas, Canceladas, Pendentes
- ✅ Gráfico de faturamento diário
- ✅ Listagem paginada de notas com filtros
- ✅ Detalhamento completo da nota (itens, impostos, eventos, datas)
- ✅ Lista de notas elegíveis a descarte (fora do período de retenção)

### Segurança
- ✅ **JWT em dois estágios:** access token sem `empresa_id` no login → seleção de empresa → token completo
- ✅ **Refresh token** com expiração e rotação
- ✅ Hash de senha com **BCrypt**
- ✅ Authorization por role (`Admin` × `User`)
- ✅ Validação cruzada `EmpresaId do token` × `EmpresaId do recurso` em todas as rotas
- ✅ Audit log de operações sensíveis com IP de origem
- ✅ **Cifragem em repouso** de senha do certificado A1 e token CSC (ASP.NET Data Protection)
- ✅ **Fail-fast** no startup quando `Jwt:Secret` é vazio, curto ou placeholder
- ✅ **RNG criptográfico** (`RandomNumberGenerator`) na geração de `cNF` e chave de acesso — impede previsão da chave por timing
- ✅ **Escape de XML** em todos os campos de texto livre (razão social, descrições, justificativas) — prevenção contra XML Injection
- ✅ **Upload de certificado restrito a Admin** com limite de tamanho (256 KB)
- ✅ **Secrets via variáveis de ambiente** (`.env` gitignored) — nenhum segredo no repositório

### Integrações auxiliares
- ✅ **ViaCEP** — autocompletar endereço por CEP
- ✅ **Envio de NF-e por e-mail** (XML + DANFE) ao destinatário
- ✅ **Health check** em `/health`

---

## 🔒 Segurança e Proteção de Dados

A plataforma trata de **dados fiscais juridicamente vinculantes** e **certificados digitais ICP-Brasil**. A postura de segurança é tratada como funcionalidade-produto, não como TODO de operação.

### Cifragem em repouso de secrets sensíveis

| Dado | Onde fica | Como é protegido |
|------|-----------|------------------|
| **Senha do certificado A1** | Coluna `empresas.CertificadoSenha` | Cifrada via **ASP.NET Data Protection API** com chaves AES-256 rotacionáveis, prefixo `enc:v1:` |
| **Token CSC (NFC-e)** | Coluna `empresas.CscToken` | Cifrado idem |
| **Bytes do certificado .pfx** | Coluna `empresas.CertificadoBytes` | Já protegido por senha PFX (PKCS#12) |
| **Senha de usuário** | Coluna `usuarios.SenhaHash` | Hash **BCrypt** (work factor 11, salt automático) |
| **JWT Secret** | Variável de ambiente `Jwt__Secret` | Nunca persistido em disco do app; fail-fast se ausente/curto |
| **Senha do PostgreSQL** | Variável `POSTGRES_PASSWORD` | Lida do `.env` (gitignored); compose falha se ausente |

As chaves de cifragem do Data Protection ficam **fora do banco** (volume Docker `dp_keys`), o que impede que um vazamento da tabela `empresas` exponha senhas/tokens em claro.

> **Rotação de chaves:** o Data Protection rotaciona automaticamente a cada 90 dias. Valores antigos continuam decifráveis enquanto a chave antiga não for purgada.
>
> **Migração de chave:** para trocar de file system para Azure Key Vault / AWS KMS, basta substituir a configuração de `AddDataProtection()` em `DependencyInjection.cs` — o `EncryptedStringConverter` é agnóstico ao backend.

### Defesa em profundidade

| Camada | Controle implementado |
|--------|----------------------|
| **Boot da API**     | Fail-fast: lança `InvalidOperationException` se `Jwt:Secret` for vazio, < 32 chars ou conter `SUA_CHAVE`/`__TROCAR`. Mesma checagem para `ConnectionStrings:DefaultConnection`. |
| **Compose**         | Sintaxe `${VAR:?mensagem}` faz `docker compose up` falhar se `.env` estiver incompleto. |
| **Autenticação**    | JWT HS256 com `Issuer` + `Audience` + `IssuerSigningKey` validados (`ValidateLifetime`, `ValidateAudience`, `ValidateIssuer` = `true`). |
| **Multi-tenant**    | Toda query filtra por `empresa_id` do claim JWT. Repositórios não expõem método cross-tenant. |
| **Autorização**     | `[Authorize(Roles = "Admin")]` em rotas críticas (upload de certificado, atualização da empresa, gestão de usuários, atualização da tabela NCM). |
| **Upload de arquivo** | Certificado limitado a **256 KB** via `[RequestSizeLimit]` + checagem explícita. Apenas `.pfx`/`.p12` aceitos. |
| **Validação ICP-Brasil** | OIDs ICP-Brasil (`2.16.76.1.2.*`) verificados na extensão de Certificate Policies do PFX. Fallback para issuer das ACs (Soluti, Certisign, Valid, Serasa). |
| **Geração de XML**  | Todos os campos de texto livre (razão social, descrição, justificativa, etc.) passam por `SecurityElement.Escape` antes de serem interpolados — bloqueia **XML Injection**. |
| **Chave de acesso NF-e** | Campo `cNF` (8 dígitos anti-enumeração) gerado com `RandomNumberGenerator.GetInt32` (CSPRNG) — não previsível por timing como ocorre com `System.Random`. |
| **Assinatura digital** | RSA-SHA256 nativa do .NET (`SignedXml`) com transformações `XmlDsigEnveloped` + `XmlDsigC14N` exigidas pela SEFAZ. |
| **Imutabilidade**   | `FiscalImmutabilityInterceptor` em EF Core bloqueia UPDATE/DELETE de NotaFiscal autorizada/cancelada na camada do banco. |
| **Auditoria**       | `IAuditService` registra ação, usuário, empresa, chave NF-e, detalhes e IP de origem em tabela própria — independente de log de aplicação. |
| **CORS**            | Origin allowlist explícita via `WebUI:BaseUrl`. |
| **Health endpoint** | `/health` é público mas não retorna dados sensíveis (apenas status). |

### Compliance e princípios atendidos

- **LGPD Art. 46 — Boas práticas de segurança técnica:** cifragem em repouso de dados sensíveis (certificado A1 → dado de identificação inequívoca da PJ), auditoria, controle de acesso.
- **LGPD Art. 50 — Boas práticas e governança:** segregação de tenants, log de operações, retenção compatível com obrigação legal.
- **ICP-Brasil:** validação de OIDs da política de certificação antes de aceitar o certificado.
- **CTN art. 173 + Lei 10.522/02:** retenção fiscal de 5 anos com bloqueio de exclusão.
- **Manual da NF-e v4.00:** assinatura, layout XML, sequenciamento, eventos e contingência aderentes.
- **OWASP ASVS L2** (referência):
  - V2 Auth: JWT validado, BCrypt para senhas, sem hardcoded secrets.
  - V5 Validation: XSD oficial + escape de XML + validações de domínio.
  - V6 Stored Cryptography: Data Protection com chave > 256 bits.
  - V7 Error Handling: mensagens de erro genéricas; sem exposição de stack trace em produção.
  - V10 Malicious code: nenhum executável de terceiros embutido; XML gerado pelo próprio app.

### Postura de "secrets nunca no código"

```
.gitignore inclui:
  appsettings.*.json (exceto appsettings.json base, sem secrets)
  secrets.json
  .env
  .env.*
  postgres_data/
  logs/
```

O `appsettings.json` versionado tem `Jwt.Secret` e `ConnectionStrings.DefaultConnection` **vazios** — o app não sobe sem `.env` configurado. O fluxo recomendado é:

```powershell
Copy-Item .env.example .env
# editar .env e gerar secrets:
#   JWT_SECRET:        openssl rand -base64 48
#   POSTGRES_PASSWORD: openssl rand -base64 24
```

---

## 🏗️ Arquitetura

```
NfeSaas/
├── src/
│   ├── Domain/          # Entidades, Enums, Interfaces de repositório (zero dependências externas)
│   │   ├── Entities/    # Escritorio, Empresa, NotaFiscal, ItemNotaFiscal, Produto, Cliente,
│   │   │                # Usuario, EventoFiscal, AuditLog, ConfiguracaoEmpresa
│   │   ├── Enums/       # TipoNota, SituacaoNota, CstIcms, CsosnIcms, TipoEventoFiscal, etc.
│   │   └── Common/      # BaseEntity, ValueObjects, validadores fiscais (CNPJ, CFOP, NCM, IE)
│   ├── Application/     # CQRS (MediatR) — Commands, Queries, DTOs, Validators
│   │   ├── Commands/    # EmitirNFe, CancelarNFe, Auth, Escritorio, Empresa,
│   │   │                # ConfiguracaoEmpresa, EventosFiscais, Produto, Cliente
│   │   ├── Queries/     # NotaFiscalQueries, ProdutoQueries, ClienteQueries, Dashboard
│   │   └── Interfaces/  # ISefazService, IXmlNFeService, IDanfeService, ICertificadoService...
│   ├── Infrastructure/  # EF Core 8 + Npgsql, Repositórios, Serviços técnicos
│   │   ├── Data/        # NfeDbContext, Configurations, Migrations, FiscalImmutabilityInterceptor
│   │   ├── Services/    # SefazService, XmlNFeService, DanfeService, CertificadoService,
│   │   │                # ImpostoCalculoService, TokenService, CepValidationService, AuditService
│   │   ├── Repositories/
│   │   └── Schemas/     # XSDs oficiais SEFAZ v4.00
│   ├── API/             # ASP.NET Core 8 — Controllers REST, Middleware, Program.cs
│   │   └── Controllers/ # Auth, Escritorio, Empresa, NotaFiscal, Produto, Cliente, Inutilizacao
│   └── WebUI/           # Blazor WebAssembly + MudBlazor 6
│       ├── Pages/       # Login, Dashboard, EmitirNFe, NotasEmitidas, NotaDetalhe,
│       │                # Empresas, Empresa, EscritorioComoEmpresa, Certificado,
│       │                # Produtos, Clientes, Usuarios, Inutilizacoes, ConfiguracaoInicial
│       ├── Shared/      # MainLayout, NavMenu
│       └── Services/    # AuthService, NotaFiscalService, EscritorioService,
│                        # PersonalizacaoService (flags semânticas da UI),
│                        # JwtAuthStateProvider
├── tests/
│   ├── NfeSaas.Tests.Unit/         # Testes de domínio e serviços isolados
│   ├── NfeSaas.Tests.Integration/  # Banco real via Testcontainers
│   └── NfeSaas.Tests.BDD/          # Cenários Gherkin (SpecFlow)
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.webui
│   └── nginx.conf
├── docker-compose.yml
└── scripts/
    └── seed.sql                    # Dados de demonstração
```

Padrões aplicados: **DDD**, **CQRS** (MediatR), **Clean Architecture**, **Repository + Unit of Work**, **Interceptors de EF Core**.

---

## ⚡ Início Rápido — Docker Compose

### Pré-requisitos
- Docker 24+ e Docker Compose V2
- (Opcional, Windows) PowerShell 5.1+ para o script `restart.ps1`

### 1. Configure os secrets (`.env`)

O compose exige variáveis de ambiente — não há credenciais default no repositório.

```powershell
Copy-Item .env.example .env
# Edite .env e gere valores fortes:
#   JWT_SECRET:        openssl rand -base64 48     (>= 32 chars)
#   POSTGRES_PASSWORD: openssl rand -base64 24
```

### 2. Suba toda a stack

**Atalho (Windows):**

```powershell
.\restart.ps1
```

O script valida o `.env`, sobe os containers, aplica migrations, semeia dados de demo (se o banco está vazio) e abre a solution no IDE. Use `-Clean` para apagar o volume do Postgres e recomeçar do zero. Outros switches: `-NoBuild`, `-SkipMigrations`, `-NoIde`, `-NoSeed`.

**Manual:**

```powershell
docker compose up -d --build
```

### 3. Aguarde a inicialização (~2 min na primeira vez)

```powershell
docker compose ps
docker compose logs -f api
```

### 3. Acesse os serviços

| Serviço          | URL                              |
|------------------|----------------------------------|
| WebUI (Blazor)   | http://localhost:5002            |
| API (Swagger)    | http://localhost:5001/swagger    |
| Health Check     | http://localhost:5001/health     |
| PostgreSQL       | localhost:5432                   |

### 4. Login de demonstração

| Campo | Valor                  |
|-------|------------------------|
| Email | admin@nfesaas.com.br   |
| Senha | Admin@123              |

Dados de seed:
- **Escritório** ID: `cccccccc-cccc-cccc-cccc-cccccccccccc` (CNPJ `99.999.999/0001-91`)
- **Empresa**    ID: `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` (CNPJ `00.000.000/0001-91`)

---

## 🖥️ Desenvolvimento Local

### Pré-requisitos
- .NET 8 SDK
- PostgreSQL 16 (ou rodar apenas o serviço `postgres` do compose)
- Docker (para Testcontainers nos testes de integração)

### 1. Configure a connection string

`src/API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=nfesaas;Username=postgres;Password=sua_senha"
  },
  "Jwt": {
    "Secret": "SUA_CHAVE_SECRETA_MUITO_LONGA_PELO_MENOS_32_CHARS",
    "Issuer": "NfeSaas",
    "Audience": "NfeSaas.WebUI"
  }
}
```

### 2. Aplique as migrations

O container da API **não tem o SDK do EF Core**. Para aplicar migrações em ambiente containerizado, gere o script SQL idempotente e aplique via `psql`:

```powershell
dotnet ef migrations script --idempotent -o migration.sql `
  --project src/Infrastructure --startup-project src/API

docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/migration.sql
```

Em dev local (Postgres host nativo) basta `dotnet ef database update --project src/Infrastructure --startup-project src/API`.

### 3. Execute API e WebUI

```powershell
# Terminal 1
dotnet run --project src/API --urls=http://localhost:5001

# Terminal 2
dotnet run --project src/WebUI --urls=http://localhost:5002
```

---

## 🔐 Modelo Multi-Tenant e Fluxo JWT

Hierarquia: **Escritório → Empresa → NotaFiscal**. Um **Usuário pertence ao Escritório** (não à Empresa), o que permite que o mesmo contador atenda múltiplos CNPJs sem credenciais duplicadas.

### Fluxo de autenticação (dois tokens)

```
1) POST /api/auth/login
   { email, senha }
   → 200 { accessToken (sem empresa_id), refreshToken, empresas[],
           assinatura: { plano, status, diasRestantesTrial, trialFimEm, planoAtivoAteEm } }
   → 401 { codigo: "CredenciaisInvalidas", message }
   → 402 { codigo: "TrialExpirado",       message, assinatura }   ← UI orienta ativar plano
   → 403 { codigo: "EscritorioSuspenso",  message, assinatura }

2) POST /api/auth/selecionar-empresa     (Authorization: Bearer <accessToken do passo 1>)
   { empresaId }
   → 200 { accessToken (com empresa_id e escritorio_id) }
   → 400 quando escritorio.PodeAcessar() == false (trial expirado, suspenso)

3) Todas as rotas protegidas exigem o token com empresa_id.
```

**Claims do token final:**
- `sub` — id do usuário
- `email`
- `role` (`Admin` | `User`)
- `escritorio_id`
- `empresa_id`

A troca de empresa via UI dispara o passo 2 novamente — **sem novo login**. Toda query/comando subsequente é filtrada pelo `empresa_id` do token.

---

## 🛡️ Conformidade Fiscal e Imutabilidade

| Garantia | Mecanismo |
|----------|-----------|
| Documento autorizado é imutável | `NotaFiscal.EnsureMutavel()` bloqueia mutação em qualquer setter quando `Situacao ∈ { Autorizada, Cancelada }` |
| Banco bloqueia UPDATE/DELETE inadequado | `FiscalImmutabilityInterceptor` (EF Core SaveChanges interceptor) |
| Retenção legal de 5 anos | `NotaFiscal.DataDescarteAutorizado` + bloqueio em `Delete()` enquanto `DentroPeriodoRetencao = true` |
| Numeração sem duplicidade | Índice único `(EmpresaId, Tipo, Série, Número, Ambiente)` no Postgres + pré-check + transação |
| Auditoria | `IAuditService` registra `Acao`, `UsuarioId`, `ChaveNFe`, `IP`, `Detalhes` em cada emissão/cancelamento/CC-e |
| Validação prévia | XSD oficial SEFAZ + validadores CNPJ/CPF/CFOP/NCM/UF/CEP/CST/CSOSN antes do envio |
| Cobertura de regime tributário | Validação cruzada CRT × CST/CSOSN — impede Simples emitir CST e Regime Normal emitir CSOSN |

---

## 📡 Endpoints da API

### Autenticação
```
POST   /api/auth/login                          Login (e-mail + senha)
                                                → 200 LoginResultDto (com `assinatura`)
                                                → 401 codigo=CredenciaisInvalidas
                                                → 402 codigo=TrialExpirado   (assinatura preenchida)
                                                → 403 codigo=EscritorioSuspenso
POST   /api/auth/refresh                        Renovar access token
POST   /api/auth/selecionar-empresa             Trocar empresa (gera novo token com empresa_id)
                                                Valida escritorio.PodeAcessar() — bloqueia trial expirado
```

### Escritório (multi-tenant)
```
POST   /api/escritorio/registrar                Auto-cadastro (público) — Trial 30 dias automático
                                                Plano obrigatório: Basico | Profissional | Enterprise
POST   /api/escritorio/cadastrar-como-empresa   Cadastrar próprio escritório como empresa emitente  [Admin]
                                                Idempotente por CNPJ; copia razão social/contato
POST   /api/escritorio/ativar-plano             Ativar plano pago (AtivoAteUtc, ValorPago?)         [Admin]
                                                Em produção: substituir por webhook do gateway
GET    /api/escritorio/empresas                 Listar empresas do escritório
POST   /api/escritorio/empresas                 Criar empresa cliente            [Admin]
GET    /api/escritorio/usuarios                 Listar usuários                  [Admin]
POST   /api/escritorio/usuarios                 Criar usuário                    [Admin]
PUT    /api/escritorio/usuarios/{id}            Atualizar usuário                [Admin]
PATCH  /api/escritorio/usuarios/{id}/toggle-ativo                                 [Admin]
DELETE /api/escritorio/usuarios/{id}            Excluir usuário                  [Admin]
```

### Empresa atual
```
GET    /api/empresa                             Dados da empresa selecionada
PUT    /api/empresa                             Atualizar empresa                [Admin]
GET    /api/empresa/certificado/status          Status do certificado A1
POST   /api/empresa/certificado/upload          Upload do .pfx/.p12 + senha
GET    /api/empresa/configuracao                Wizard de personalização
POST   /api/empresa/configuracao                Salvar personalização
```

### Notas fiscais
```
GET    /api/notas-fiscais                       Listar (paginado)
GET    /api/notas-fiscais/{id}                  Detalhe completo
POST   /api/notas-fiscais/emitir                Emitir NF-e ou NFC-e
POST   /api/notas-fiscais/{id}/cancelar         Cancelar (com justificativa)
POST   /api/notas-fiscais/{id}/cce              Carta de Correção Eletrônica
POST   /api/notas-fiscais/{id}/manifestar       Manifestação do destinatário
GET    /api/notas-fiscais/{id}/eventos          Eventos da chave
GET    /api/notas-fiscais/{id}/danfe            Download PDF do DANFE
GET    /api/notas-fiscais/{id}/xml              Download XML
GET    /api/notas-fiscais/dashboard?ano&mes     KPIs e faturamento diário
GET    /api/notas-fiscais/elegiveis-descarte    Notas fora do prazo de retenção
```

### Cadastros
```
GET    /api/produtos[?apenasAtivos]
GET    /api/produtos/{id}
POST   /api/produtos
PUT    /api/produtos/{id}
PATCH  /api/produtos/{id}/toggle-ativo
DELETE /api/produtos/{id}

GET    /api/clientes[?apenasAtivos]
GET    /api/clientes/{id}
POST   /api/clientes
PUT    /api/clientes/{id}
PATCH  /api/clientes/{id}/toggle-ativo
DELETE /api/clientes/{id}
```

### Eventos fiscais
```
GET    /api/inutilizacoes                       Listar inutilizações da empresa
POST   /api/inutilizacoes                       Inutilizar range de numeração
```

### Diagnóstico
```
GET    /health                                  Health check (público)
GET    /api/diagnostics/xsd                     Status de carregamento dos XSDs
```

A documentação interativa fica em **http://localhost:5001/swagger**.

---

## 💰 Impostos e Regimes Tributários Suportados

### Regimes
| CRT | Regime | Tabela usada |
|-----|--------|--------------|
| 1   | Simples Nacional | **CSOSN** (101, 102, 103, 201, 202, 203, 300, 400, 500, 900) |
| 2   | Simples Nacional — excesso de sublimite | **CSOSN** |
| 3   | Regime Normal (Lucro Real/Presumido) | **CST** (00, 10, 20, 30, 40, 50, 51, 60, 70, 90) |

A emissão valida que a combinação CRT × CST/CSOSN é coerente — impede falhas comuns na rejeição da SEFAZ.

### Tributos
| Imposto    | Funcionalidades |
|------------|-----------------|
| **ICMS**     | CST 00-90, base reduzida, diferimento, isenção, suspensão |
| **ICMS-ST**  | MVA, alíquota interna do destino vs interestadual |
| **DIFAL**    | Cálculo automático em operação interestadual a consumidor final (sem IE) |
| **FCP**      | Fundo de Combate à Pobreza, calculado sobre BC do ICMS |
| **PIS**      | CST 01-99, alíquota percentual |
| **COFINS**   | CST 01-99, alíquota percentual |
| **IPI**      | Opcional por item, CST configurável |

### Origens de mercadoria suportadas
Nacional, estrangeira (importação direta / mercado interno / sem similar nacional), nacional com conteúdo importação 0–40%, 40–70%, >70%, processos básicos.

---

## 📜 Eventos Fiscais Suportados

| Evento | Código tpEvento | Descrição |
|--------|----------------|-----------|
| Cancelamento                       | 110111 | Cancela uma NF-e autorizada (janela 24h) |
| Carta de Correção Eletrônica       | 110110 | Até 20 sequenciais por chave |
| Inutilização de numeração          | NfeInutilizacao | Range de números por série/ano |
| Manifestação — Confirmação         | 210200 | Destinatário confirma a operação |
| Manifestação — Ciência             | 210210 | Destinatário toma ciência da operação |
| Manifestação — Desconhecimento     | 210220 | Destinatário desconhece a operação |
| Manifestação — Não Realizada       | 210240 | Destinatário declara que a operação não ocorreu |

Cada evento é **assinado digitalmente** com a estrutura SEFAZ correta (`<Signature>` como irmão de `<infEvento>` para eventos; irmão de `<infInut>` para inutilização).

---

## 🛟 Contingência SEFAZ

Quando o webservice autorizadora da UF está fora do ar, a `NotaFiscal` pode ser marcada com:

| Tipo emissão | Código | Quando usar |
|--------------|--------|-------------|
| Normal                | 1 | SEFAZ da UF operando normalmente |
| SVC-AN                | 9 | SEFAZ Virtual de Contingência — Ambiente Nacional |
| SVC-RS                | 6 | SEFAZ Virtual de Contingência — Rio Grande do Sul |
| FS-DA                 | 5 | Formulário de Segurança — Documento Auxiliar |

A nota é persistida com a marcação e segue o ciclo normal de eventos quando a SEFAZ autorizadora volta.

---

## 🛠️ Tecnologias

| Camada         | Tecnologia |
|----------------|------------|
| Backend        | .NET 8, ASP.NET Core 8 |
| ORM            | Entity Framework Core 8 + Npgsql |
| Banco de Dados | PostgreSQL 16 |
| Mensageria     | MediatR 12 (CQRS) |
| Validação      | FluentValidation 11 |
| Auth           | JWT Bearer + BCrypt |
| XML NF-e       | System.Xml + RSA X509 (criptografia nativa .NET) |
| DANFE PDF      | QuestPDF (Community License) |
| SEFAZ          | SOAP/HTTP com mTLS (certificado A1) |
| Frontend       | Blazor WebAssembly 8 + MudBlazor 6.20 |
| Logging        | Serilog (Console + File rolling) |
| Containers     | Docker + Docker Compose + Nginx |
| Testes         | xUnit + SpecFlow (BDD) + Testcontainers |

---

## ⚙️ Variáveis de Ambiente

Configuradas via `.env` na raiz (ver `.env.example`). O `docker-compose.yml` usa interpolação `${VAR:?msg}` e falha se a variável for ausente.

### Obrigatórias

| Variável | Onde aparece | Descrição |
|----------|--------------|-----------|
| `POSTGRES_DB` | compose | Nome do banco (sugerido: `nfesaas`) |
| `POSTGRES_USER` | compose | Usuário do banco |
| `POSTGRES_PASSWORD` | compose | Senha forte do banco (gere com `openssl rand -base64 24`) |
| `JWT_SECRET` | compose → `Jwt__Secret` | Chave secreta JWT — mínimo **32 caracteres**, fail-fast no startup se inválida (gere com `openssl rand -base64 48`) |

### Opcionais (com default)

| Variável | Default | Descrição |
|----------|---------|-----------|
| `JWT_ISSUER` | `NfeSaas` | Issuer do token |
| `JWT_AUDIENCE` | `NfeSaas.WebUI` | Audience do token |
| `WEBUI_BASE_URL` | `http://localhost:5002` | URL pública da WebUI (CORS allowlist) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | `Development` habilita Swagger UI |
| `DataProtection__KeysPath` | `/app/dpkeys` | Path interno onde as chaves de cifragem ficam persistidas |
| `Ncm__UpdateSourceUrl` | _vazio_ | URL HTTPS para atualização semanal da tabela NCM (Portal Único Siscomex) |
| `Ncm__LocalFilePath` | _vazio_ | Path interno para JSON local de NCM (alternativa à URL) |
| `Ncm__UpdateIntervalDays` | `7` | Intervalo entre atualizações automáticas |
| `Ncm__UpdateOnStartup` | `false` | Se `true`, atualiza tabela NCM no boot |

### Volumes Docker importantes

| Volume | Caminho no container | Finalidade |
|--------|---------------------|------------|
| `postgres_data` | `/var/lib/postgresql/data` | Dados do PostgreSQL. **Backup crítico**. |
| `dp_keys` | `/app/dpkeys` | Chaves de cifragem (Data Protection). **Perder = perder acesso a `CertificadoSenha` e `CscToken` já gravados**. Backup crítico. |
| `api_logs` | `/app/logs` | Logs Serilog rotacionados por dia. |

---

## 🧪 Testes

```powershell
# Testes de domínio e serviços isolados
dotnet test tests/NfeSaas.Tests.Unit

# Integração (sobe Postgres real via Testcontainers — precisa do Docker)
dotnet test tests/NfeSaas.Tests.Integration

# BDD com cenários Gherkin (SpecFlow)
dotnet test tests/NfeSaas.Tests.BDD

# Todos
dotnet test
```

Os testes de integração e BDD **não dependem** do `docker compose` rodando — Testcontainers sobe o Postgres sob demanda.

Convenção: testes que exercitam SEFAZ usam sempre `AmbienteSefaz.Homologacao` com o stub embutido.

---

## 🚀 Publicação em Produção

Guia operacional para colocar a plataforma em ambiente produtivo emitindo NF-e/NFC-e reais contra a SEFAZ.

### 1. Hardening do host

| Item | Recomendação |
|------|-------------|
| Sistema operacional | Linux LTS (Ubuntu 22.04+ / Debian 12 / RHEL 9). Patches automáticos habilitados. |
| Firewall | Apenas `443/tcp` (HTTPS) e `22/tcp` (SSH com chave). **Não exponha** `5432` (Postgres) nem `8080` (API direta) na internet. |
| SSH | Apenas chave pública, sem senha. Fail2ban opcional. |
| Usuário do app | Não-root, sem acesso sudo. Dono dos volumes Docker. |
| Atualização do Docker | Docker Engine 24+, Compose V2 plugin (não a CLI legacy). |

### 2. Configuração de secrets

**Nunca** use valores do `.env.example` em produção. Gere novos:

```bash
# JWT (>= 32 chars; recomendado 64+)
openssl rand -base64 48

# Senha do Postgres
openssl rand -base64 24
```

Armazene o `.env` com permissão `chmod 600` e dono = usuário do app. Em ambientes corporativos prefira:

- **HashiCorp Vault / AWS Secrets Manager / Azure Key Vault** — injete via `docker run --env-file <(vault kv get …)` ou via init container.
- **Docker Swarm Secrets** se rodar em Swarm: trocar `${JWT_SECRET}` por `/run/secrets/jwt_secret` montado.
- **Kubernetes**: `Secret` montado como env ou arquivo.

### 3. Cifragem em repouso

A senha do certificado A1 e o token CSC ficam cifrados no banco via Data Protection. As chaves de cifragem ficam no volume `dp_keys`. Em produção, evolua para um Key Management externo:

| Backend | Implementação | Vantagem |
|---------|--------------|----------|
| Volume Docker (default) | `PersistKeysToFileSystem` | Simples, sem dependência externa |
| Azure Key Vault | `ProtectKeysWithAzureKeyVault` + `PersistKeysToAzureBlobStorage` | HSM-backed, auditoria nativa |
| AWS KMS | wrapper customizado em `IXmlEncryptor` | Integração com IAM |
| Postgres | `PersistKeysToDbContext` + tabela própria | Reduz superfície (mesma rede que dados) |

**Backup das chaves é crítico.** Sem `dp_keys`, certificados e tokens já cifrados ficam ilegíveis.

### 4. HTTPS / TLS obrigatório

A API e a WebUI **devem** ficar atrás de um reverse proxy com TLS terminado:

```nginx
# /etc/nginx/conf.d/nfesaas.conf
server {
    listen 443 ssl http2;
    server_name app.seu-dominio.com.br;

    ssl_certificate     /etc/letsencrypt/live/app.seu-dominio.com.br/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.seu-dominio.com.br/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;

    # HSTS — força HTTPS por 6 meses
    add_header Strict-Transport-Security "max-age=15552000; includeSubDomains" always;

    # Headers de segurança recomendados
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header Referrer-Policy "no-referrer" always;

    location /api/ {
        proxy_pass http://127.0.0.1:5001/;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto https;
        proxy_set_header X-Real-IP $remote_addr;
        client_max_body_size 1m;
    }

    location / {
        proxy_pass http://127.0.0.1:5002/;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto https;
    }
}
```

Atualize o `.env` com a URL pública:

```
WEBUI_BASE_URL=https://app.seu-dominio.com.br
```

> Use **Let's Encrypt** + **certbot** para certificados gratuitos auto-renovados.

### 5. Migrations (não use migrate-on-startup em produção)

Por padrão a API roda `Database.MigrateAsync()` no boot, o que **não** é seguro em ambiente com múltiplas réplicas. Em produção, aplique as migrations em um job dedicado **antes** de subir a nova versão da API:

```powershell
# Na máquina de deploy, com o .NET SDK instalado:
dotnet ef migrations script --idempotent -o migration.sql `
  --project src/Infrastructure --startup-project src/API

# Copie para o container postgres e aplique:
docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
docker exec nfesaas_postgres psql -U $env:POSTGRES_USER -d $env:POSTGRES_DB `
  -v ON_ERROR_STOP=1 -f /tmp/migration.sql

# Depois suba a API
docker compose up -d --build api webui
```

Para desabilitar o migrate-on-startup, comente o bloco em `src/API/Program.cs:91-96` antes do build da imagem de produção.

### 6. Backup e Disaster Recovery

| Componente | Frequência | Comando |
|-----------|-----------|---------|
| **PostgreSQL** | Diário (full) + WAL contínuo | `docker exec nfesaas_postgres pg_dump -U $POSTGRES_USER -d $POSTGRES_DB -F c -f /tmp/db.dump` |
| **`dp_keys`** | Diário | `docker run --rm -v nfe-saas_dp_keys:/data -v "$PWD:/backup" alpine tar czf /backup/dp_keys.tgz -C /data .` |
| **`.env`** | Após cada alteração | Cofre de senhas (1Password / Vault / KMS) |
| **Logs** | Conforme política | Centralize via Filebeat / Vector / Promtail |

Teste a restauração **trimestralmente** — backup não testado não é backup.

### 7. Observabilidade

| Sinal | Como expor |
|-------|-----------|
| Health | `GET /health` (público) — use no load balancer |
| Métricas | Adicione `App.Metrics` ou `OpenTelemetry` (não bundleado) |
| Logs estruturados | Serilog já grava JSON em `/app/logs/nfesaas-YYYYMMDD.log` — encaminhe para ELK/Loki |
| Auditoria fiscal | Tabela `audit_logs` (mantenha 5+ anos) |
| Alertas críticos | Certificado A1 expira (campo `CertificadoValidade`); SEFAZ retorna `cStat` de rejeição persistente; fila de notas pendentes > N |

### 8. Operação fiscal

| Tarefa | Cadência |
|--------|----------|
| Monitorar **validade do certificado A1** por empresa | Diário (alerta T-30, T-15, T-7) |
| Atualizar **tabela NCM** oficial | Semanal (worker já agendado — configure `Ncm__UpdateSourceUrl`) |
| Validar **status do serviço SEFAZ** | A cada emissão (já integrado via `ISefazService.ConsultarStatusServicoAsync`) |
| Acompanhar **lista de notas pendentes** (`Situacao = Enviada`) | Diário — pode indicar problema de comunicação |
| Conferir **integridade dos XMLs assinados** | Antes de qualquer descarte (após retenção legal) |

### 9. Performance e escala

- **Postgres:** habilite `pg_stat_statements`, `autovacuum` agressivo nas tabelas `notas_fiscais` e `audit_logs`. Considere particionamento por ano para volumes > 1M notas.
- **API stateless:** múltiplas réplicas atrás do load balancer. Stickiness não é necessária — JWT carrega o contexto.
- **WebUI:** Blazor WASM serve estaticamente via Nginx; cache agressivo (`Cache-Control: public, max-age=31536000, immutable`) para arquivos versionados.
- **Sessão Data Protection:** com múltiplas réplicas da API, o volume `dp_keys` precisa ser **compartilhado** (NFS, EFS) ou migrado para backend distribuído (Postgres / Azure Key Vault).

### 10. Rotação de secrets

| Secret | Quando rotacionar | Procedimento |
|--------|-------------------|--------------|
| `JWT_SECRET` | A cada 90 dias ou ao suspeitar de vazamento | Atualize `.env`, restart API. **Todos os tokens emitidos são invalidados** — usuários precisam logar de novo. |
| `POSTGRES_PASSWORD` | A cada 180 dias | `ALTER USER nfesaas WITH PASSWORD '<nova>'` no banco + atualizar `.env` + `docker compose up -d` para api. |
| Chaves Data Protection | Automático (90 dias) | Nenhuma ação. Valores cifrados com chaves antigas continuam decifrando enquanto não-purgados. |
| Certificado A1 | Conforme expiração ICP-Brasil (~1 ano A1) | Upload pela WebUI (página _Certificado_). |
| Token CSC NFC-e | Conforme rotação SEFAZ | Atualize via WebUI (página _Empresa → Configuração_). |

### 11. Checklist de Go-Live

- [ ] `.env` gerado com secrets fortes e armazenado em cofre
- [ ] HTTPS configurado no reverse proxy com certificado válido
- [ ] DNS apontando para o host
- [ ] Postgres em servidor dedicado (ou managed: RDS/Cloud SQL/Azure DB) com backup automático
- [ ] Volume `dp_keys` com backup configurado
- [ ] `WEBUI_BASE_URL` apontando para o domínio público
- [ ] Migrations aplicadas via script (não migrate-on-startup)
- [ ] Migrate-on-startup desabilitado em `Program.cs`
- [ ] Logs sendo agregados (ELK / Loki / CloudWatch)
- [ ] Alerta de saúde no `/health` configurado
- [ ] Alerta de expiração de certificado A1 (T-30 dias)
- [ ] Política de retenção de banco e logs ≥ 5 anos
- [ ] Procedimento de restore testado em ambiente de homologação
- [ ] Usuário admin inicial criado, senha forte, MFA externo (se aplicável no IDP do cliente)
- [ ] Cadastro real das empresas clientes feito (CNPJ, IE, CNAE, endereço, certificado A1)
- [ ] Emissão piloto em **AmbienteSefaz.Homologacao** validada
- [ ] Transição para `AmbienteSefaz.Producao` após homologação aceita pela SEFAZ

### 12. Compliance fiscal

- **Retenção legal:** 5 anos contados do exercício seguinte ao da emissão (CTN art. 173). O sistema bloqueia DELETE enquanto `DentroPeriodoRetencao = true` e expõe `DataDescarteAutorizado` para auditoria.
- **Manual de Orientação do Contribuinte v7.0+** (NF-e v4.00): layout, validações e regras de eventos seguidos integralmente.
- **NFC-e:** registre CSC no painel SEFAZ da UF emissora; configure CscId/CscToken na empresa antes de emitir (página _Empresa → Configuração_).
- **Contingência:** ative `TipoEmissao` apropriado (SVC-AN / SVC-RS / FS-DA) quando a SEFAZ autorizadora estiver fora — não emita "normal" forçado durante outage.

---

## 📞 Suporte e Diagnóstico

- **Swagger UI:** http://localhost:5001/swagger
- **Health:** http://localhost:5001/health
- **Status dos XSDs:** http://localhost:5001/api/diagnostics/xsd
- **Logs API:** `docker compose logs -f api`
- **Logs DB:** `docker compose logs -f postgres`
