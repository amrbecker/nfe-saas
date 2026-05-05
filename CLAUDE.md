# NfeSaas — Instruções para Claude Code

## Visão Geral

SaaS multi-tenant para emissão de NF-e e NFC-e, usado por escritórios de contabilidade.
Hierarquia: **Escritório → Empresa → NotaFiscal**. Usuários pertencem ao Escritório, não à Empresa.

## Stack

| Camada | Tecnologia |
|--------|-----------|
| API | ASP.NET Core 8 + MediatR + EF Core 8 |
| Frontend | Blazor WASM + MudBlazor 6.20.0 |
| Banco | PostgreSQL 16 |
| Auth | JWT com dois tokens (veja fluxo abaixo) |
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

1. `POST /api/auth/login` → retorna `accessToken` sem `empresa_id` + lista de Empresas
2. `POST /api/auth/selecionar-empresa` → retorna novo `accessToken` com `empresa_id` e `escritorio_id`
3. Todas as rotas protegidas exigem o token com `empresa_id`

## Convenções de Código

- **Domínio em português**: nomes de entidades, propriedades, DTOs e labels de UI seguem PT-BR (Escritório, Empresa, NotaFiscal, Destinatario, etc.)
- **Infraestrutura em inglês**: namespaces, padrões técnicos, nomes de controllers usam convenções inglesas
- Entidades de domínio têm construtores `protected` + factory method estático (`Criar(...)`)
- Propriedades de entidades são `private set` — mutação ocorre apenas via métodos da entidade
- Commands e Queries usam `record` com MediatR
- Repositórios sempre recebem `CancellationToken ct = default`

## Banco de Dados e Migrações

O container da API **não tem o SDK do EF Core**. Para aplicar migrações:

```powershell
# 1. Gerar script SQL idempotente (rodar na máquina host)
dotnet ef migrations script --idempotent -o migration.sql `
  --project src/Infrastructure --startup-project src/API

# 2. Copiar para o container do postgres e aplicar
docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/migration.sql
```

Nunca rodar `dotnet ef database update` diretamente nos containers.

## Executar os Serviços

```powershell
# Subir tudo (build + start)
docker compose up -d --build

# Ver logs da API
docker compose logs -f api

# Apenas o banco (útil para dev local)
docker compose up -d postgres
```

Endpoints locais:
- API: http://localhost:5001
- Swagger: http://localhost:5001/swagger
- WebUI: http://localhost:5002
- Health: http://localhost:5001/health

## Executar Testes

```powershell
# Unitários
dotnet test tests/NfeSaas.Tests.Unit

# Integração (precisa do Docker para Testcontainers)
dotnet test tests/NfeSaas.Tests.Integration

# BDD (SpecFlow)
dotnet test tests/NfeSaas.Tests.BDD

# Todos
dotnet test
```

Os testes de integração e BDD sobem o banco via Testcontainers — não precisam do Docker Compose rodando.

## Dados de Seed (Demo)

| Recurso | Valor |
|---------|-------|
| Escritório ID | `cccccccc-cccc-cccc-cccc-cccccccccccc` |
| Escritório CNPJ | `99.999.999/0001-91` |
| Empresa ID | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| Empresa CNPJ | `00.000.000/0001-91` |
| Usuário admin | `admin@nfesaas.com.br` / `Admin@123` |

## Variáveis de Ambiente Importantes

| Variável | Descrição |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | Connection string PostgreSQL |
| `Jwt__Secret` | Segredo JWT (mín. 32 chars) |
| `Jwt__Issuer` | `NfeSaas` |
| `Jwt__Audience` | `NfeSaas.WebUI` |
| `WebUI__BaseUrl` | URL da WebUI para CORS |

## Isolamento Multi-Tenant

- Todo acesso a dados deve filtrar por `EmpresaId` (obtido do claim JWT)
- Repositórios **nunca** retornam dados de outras empresas
- Ao criar novos endpoints ou queries, sempre verificar se o `EmpresaId` do token bate com o recurso solicitado

## Serviços Externos

- **ISefazService**: stub em dev/homologação, real em produção — sempre usar `AmbienteSefaz.Homologacao` nos testes
- **IXmlNFeService**: geração e assinatura XML da NF-e (exige certificado digital na Empresa)
- **IImpostoCalculoService**: cálculo de ICMS, ICMS-ST, PIS e COFINS — testado via testes unitários isolados

## Padrões a Seguir

- Novos casos de uso → novo Command/Query em `Application/`, handler correspondente, interface no repositório se necessário
- Novos campos no banco → nova migration EF (`dotnet ef migrations add NomeMigration`)
- Não adicionar lógica de negócio nos Controllers — apenas despachar para MediatR
- Não usar `DateTime.Now` no domínio — usar `DateTime.UtcNow`
