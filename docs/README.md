# 🧾 NfeSaas — Sistema SaaS de Emissão de NF-e/NFC-e

Sistema completo para emissão de Nota Fiscal Eletrônica (NF-e modelo 55) e Nota Fiscal de Consumidor Eletrônica (NFC-e modelo 65) integrado ao webservice da SEFAZ.

---

## 🏗️ Arquitetura

```
NfeSaas/
├── src/
│   ├── Domain/          # Entidades, enums, interfaces de repositório
│   ├── Application/     # CQRS (Commands/Queries), DTOs, MediatR, FluentValidation
│   ├── Infrastructure/  # EF Core, Repositórios, Serviços (SEFAZ, XML, DANFE, JWT)
│   ├── API/             # Controllers REST, Middleware, Program.cs
│   └── WebUI/           # Blazor WebAssembly + MudBlazor
├── docker/
│   ├── docker-compose.yml
│   ├── Dockerfile.api
│   ├── Dockerfile.webui
│   └── nginx.conf
├── scripts/
│   └── seed.sql         # Dados iniciais
└── docs/README.md
```

---

## ⚡ Início Rápido — Docker Compose

### Pré-requisitos
- Docker 24+ e Docker Compose V2
- (Opcional) .NET 8 SDK para desenvolvimento local

### 1. Suba toda a stack

```bash
# Na raiz do projeto
cd /caminho/para/nfe-saas

docker compose -f docker/docker-compose.yml up -d --build
```

### 2. Aguarde os serviços iniciarem (~2 min na primeira vez)

```bash
docker compose -f docker/docker-compose.yml ps
docker compose -f docker/docker-compose.yml logs api -f
```

### 3. Acesse

| Serviço       | URL                          |
|---------------|------------------------------|
| WebUI (Blazor)| http://localhost:5002        |
| API (Swagger) | http://localhost:5001/swagger |
| Health Check  | http://localhost:5001/health  |
| PostgreSQL     | localhost:5432               |

### 4. Login padrão

| Campo | Valor                  |
|-------|------------------------|
| Email | admin@nfesaas.com.br   |
| Senha | Admin@123              |

---

## 🖥️ Desenvolvimento Local

### Pré-requisitos
- .NET 8 SDK
- PostgreSQL 14+ rodando localmente

### 1. Configure a connection string

Edite `src/API/appsettings.Development.json`:

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

### 2. Crie o banco e rode as migrations

```bash
cd src/Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../API
dotnet ef database update --startup-project ../API
```

### 3. Execute API e WebUI

```bash
# Terminal 1 — API
cd src/API
dotnet run --urls=http://localhost:5001

# Terminal 2 — WebUI
cd src/WebUI
dotnet run --urls=http://localhost:5002
```

---

## 🔐 Certificado Digital A1

1. Acesse **Menu → Certificado**
2. Faça upload do arquivo `.pfx` ou `.p12`
3. Informe a senha do certificado
4. O sistema valida e armazena de forma segura

> ⚠️ **Homologação:** Para testes, use certificado de teste da SEFAZ. O ambiente de homologação não exige certificado real, mas o sistema valida o formato.

---

## 📋 Fluxo de Emissão NF-e

```
Preenchimento → Validação XML → Assinatura Digital → Envio SEFAZ → Retorno (Autorizada/Rejeitada)
```

1. **Configuração:** Tipo (NF-e/NFC-e), Operação, Finalidade
2. **Destinatário:** CPF/CNPJ, endereço completo
3. **Produtos:** Código, NCM, CFOP, quantidades, valores, impostos (ICMS/PIS/COFINS)
4. **Transporte/Pagamento:** Modalidade de frete, forma de pagamento
5. **Emissão:** Geração do XML v4.00, assinatura RSA, envio SEFAZ

---

## 🧮 Impostos Suportados

| Imposto    | Funcionalidades |
|------------|-----------------|
| **ICMS**   | CST 00-90, Base reduzida, Diferimento |
| **ICMS-ST** | MVA, Alíquota interna vs interestadual |
| **PIS**    | CST 01-99, Alíquota percentual |
| **COFINS** | CST 01-99, Alíquota percentual |

---

## 🌐 Estados SEFAZ suportados

| UF | Webservice próprio |
|----|-------------------|
| SP | ✅ |
| RS | ✅ |
| Demais | ✅ via SVRS (sefazrs.rs.gov.br) |

---

## 📊 Endpoints API principais

```
POST   /api/auth/login                    — Autenticar
POST   /api/auth/refresh                  — Renovar token
GET    /api/notas-fiscais                 — Listar notas (paginado)
GET    /api/notas-fiscais/{id}            — Detalhe da nota
POST   /api/notas-fiscais/emitir          — Emitir nova nota
POST   /api/notas-fiscais/{id}/cancelar   — Cancelar nota
GET    /api/notas-fiscais/{id}/danfe      — Download DANFE PDF
GET    /api/notas-fiscais/{id}/xml        — Download XML
GET    /api/notas-fiscais/dashboard       — Métricas dashboard
GET    /api/empresa                       — Dados da empresa
GET    /api/empresa/certificado/status    — Status do certificado
POST   /api/empresa/certificado/upload    — Upload certificado A1
GET    /health                            — Health check
```

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
| XML NF-e       | System.Xml + RSA X509 (nativo .NET) |
| DANFE PDF      | QuestPDF (Community License) |
| SEFAZ          | SOAP/HTTP com mTLS (certificado A1) |
| Frontend       | Blazor WebAssembly 8 + MudBlazor 6 |
| Logging        | Serilog (Console + File rolling) |
| Containers     | Docker + Nginx |

---

## ⚙️ Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL |
| `Jwt__Secret` | Chave secreta JWT (mínimo 32 chars) |
| `Jwt__Issuer` | Issuer do token |
| `Jwt__Audience` | Audience do token |
| `WebUI__BaseUrl` | URL do frontend (para CORS) |
| `ASPNETCORE_ENVIRONMENT` | `Development` ou `Production` |

---

## 🚨 Produção — Considerações Importantes

1. **Altere o `Jwt__Secret`** para uma chave forte e única
2. **Use HTTPS** em produção (configure certificado TLS no Nginx/reverse proxy)
3. **Backup regular** do banco PostgreSQL e dos certificados A1
4. **Monitor de validade** do certificado A1 — renove antes do vencimento
5. **Logs** são gravados em `logs/nfesaas-YYYYMMDD.log` na API
6. **NF-e v4.00** — o sistema usa o layout mais recente do SEFAZ

---

## 📞 Suporte

- Swagger UI: http://localhost:5001/swagger (ambiente dev)
- Health: http://localhost:5001/health
- Logs da API: `docker compose logs api -f`
- Logs do DB: `docker compose logs postgres -f`
