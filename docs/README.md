# 🧾 NfeSaas — SaaS Multi-Tenant de Emissão de NF-e e NFC-e

Plataforma completa para emissão, gestão e arquivamento de **Nota Fiscal Eletrônica (NF-e modelo 55)** e **Nota Fiscal de Consumidor Eletrônica (NFC-e modelo 65)**, projetada para **escritórios de contabilidade** que precisam emitir documentos fiscais em nome de **múltiplas empresas clientes** sob uma única operação — com isolamento estrito de dados, conformidade fiscal, contingência SEFAZ e arquitetura moderna.

> **Quem usa:** contadores, BPOs fiscais, ERPs SaaS e empresas que precisam de uma camada fiscal embarcável.
> **Para o que serve:** substituir emissores monolíticos, planilhas e integrações ponto-a-ponto com a SEFAZ por uma plataforma única, multi-empresa, com auditoria e conformidade fim a fim.

---

## 📑 Sumário

1. [Diferenciais Competitivos](#-diferenciais-competitivos)
2. [Problemas que o Produto Resolve](#-problemas-que-o-produto-resolve)
3. [Funcionalidades](#-funcionalidades)
4. [Arquitetura](#%EF%B8%8F-arquitetura)
5. [Início Rápido — Docker Compose](#-início-rápido--docker-compose)
6. [Desenvolvimento Local](#%EF%B8%8F-desenvolvimento-local)
7. [Modelo Multi-Tenant e Fluxo JWT](#-modelo-multi-tenant-e-fluxo-jwt)
8. [Conformidade Fiscal e Imutabilidade](#-conformidade-fiscal-e-imutabilidade)
9. [Endpoints da API](#-endpoints-da-api)
10. [Impostos e Regimes Tributários Suportados](#-impostos-e-regimes-tributários-suportados)
11. [Eventos Fiscais Suportados](#-eventos-fiscais-suportados)
12. [Contingência SEFAZ](#%EF%B8%8F-contingência-sefaz)
13. [Tecnologias](#%EF%B8%8F-tecnologias)
14. [Variáveis de Ambiente](#%EF%B8%8F-variáveis-de-ambiente)
15. [Testes](#-testes)
16. [Produção](#-produção)

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
| 12 | **Auto-cadastro de escritório** | Onboarding sem fricção: o contador cria seu próprio tenant pela WebUI, sem precisar de um vendedor. |
| 13 | **Wizard de personalização** | Configuração inicial por empresa (perfil de cliente, tipo de produto, volume, nível de automação) que adapta a UI ao caso de uso. |
| 14 | **Open-source self-hosted** | Stack 100% containerizada (Docker Compose). **Sem custo por nota emitida**, sem amarração a fornecedor SaaS terceiro, dados ficam na infraestrutura do cliente. |
| 15 | **Arquitetura limpa e testável** | DDD + CQRS (MediatR) + Clean Architecture + EF Core 8. Testes Unit, Integration (Testcontainers) e BDD (SpecFlow) prontos. |
| 16 | **Mensagens 100% em PT-BR** | Domínio, UI e mensagens de erro em português brasileiro, alinhadas ao vocabulário fiscal nacional. |

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
- ✅ Auto-cadastro de Escritório (CNPJ, e-mail, telefone, plano)
- ✅ Cadastro ilimitado de Empresas (CNPJs clientes) por Escritório
- ✅ Gestão de Usuários por Escritório (roles `Admin` e `User`, ativar/desativar/excluir)
- ✅ Seletor de empresa no header (troca de contexto sem novo login)
- ✅ Wizard de Configuração Inicial por empresa (perfil cliente, volume, automação)
- ✅ Cadastro completo da Empresa (Razão Social, CNPJ, IE, IM, CNAE, endereço, regime tributário, ambiente SEFAZ)

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

### Integrações auxiliares
- ✅ **ViaCEP** — autocompletar endereço por CEP
- ✅ **Envio de NF-e por e-mail** (XML + DANFE) ao destinatário
- ✅ **Health check** em `/health`

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
│       │                # Empresas, Empresa, Certificado, Produtos, Clientes, Usuarios,
│       │                # Inutilizacoes, ConfiguracaoInicial
│       ├── Shared/      # MainLayout, NavMenu
│       └── Services/    # AuthService, NotaFiscalService, EscritorioService, JwtAuthStateProvider
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

### 1. Suba toda a stack

```powershell
docker compose up -d --build
```

### 2. Aguarde a inicialização (~2 min na primeira vez)

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
   → 200 { accessToken (sem empresa_id), refreshToken, empresas[] }

2) POST /api/auth/selecionar-empresa     (Authorization: Bearer <accessToken do passo 1>)
   { empresaId }
   → 200 { accessToken (com empresa_id e escritorio_id) }

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
POST   /api/auth/refresh                        Renovar access token
POST   /api/auth/selecionar-empresa             Trocar empresa (gera novo token com empresa_id)
```

### Escritório (multi-tenant)
```
POST   /api/escritorio/registrar                Auto-cadastro de escritório (público)
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

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL |
| `Jwt__Secret`            | Chave secreta JWT (mínimo 32 chars) |
| `Jwt__Issuer`            | Issuer do token (padrão `NfeSaas`) |
| `Jwt__Audience`          | Audience do token (padrão `NfeSaas.WebUI`) |
| `WebUI__BaseUrl`         | URL pública da WebUI (CORS) |
| `ASPNETCORE_ENVIRONMENT` | `Development` ou `Production` |

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

## 🚨 Produção

1. **Altere `Jwt__Secret`** para uma chave forte e única — nunca use o valor de exemplo do compose.
2. **Use HTTPS** em produção (configure certificado TLS no Nginx ou reverse proxy à frente da API).
3. **Backup regular** do PostgreSQL e dos certificados A1 (estão armazenados criptografados na tabela `empresas`).
4. **Monitor de validade do certificado A1** — renove antes da expiração (o sistema bloqueia emissão automaticamente).
5. **Logs** rotacionam em `logs/nfesaas-YYYYMMDD.log` na API e ficam em `/var/lib/docker/volumes/api_logs` no container.
6. **NF-e v4.00** — layout oficial vigente. Schemas XSD vivem em `src/Infrastructure/Schemas/`.
7. **Retenção de dados:** documentos autorizados/cancelados ficam protegidos por 5 anos por força de lei — provisione armazenamento adequado.
8. **Auditoria:** preserve `AuditLog` por no mínimo o mesmo período legal de retenção das notas.

---

## 📞 Suporte e Diagnóstico

- **Swagger UI:** http://localhost:5001/swagger
- **Health:** http://localhost:5001/health
- **Status dos XSDs:** http://localhost:5001/api/diagnostics/xsd
- **Logs API:** `docker compose logs -f api`
- **Logs DB:** `docker compose logs -f postgres`
