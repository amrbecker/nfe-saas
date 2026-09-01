# NfeSaas

Multi-tenant SaaS for issuing Brazilian electronic invoices (NF-e model 55 and NFC-e model 65),
built for accounting firms that file on behalf of many client companies under one account.
Handles the full fiscal lifecycle — authorization, cancellation, correction letters, number
voiding and SEFAZ contingency — behind a strict per-tenant data boundary.

> Portuguese-language deep dive (feature list, fiscal rules, API reference):
> [`docs/README.md`](docs/README.md). Production deployment: [`docs/deploy-producao.md`](docs/deploy-producao.md).

**A note on language:** the domain model is deliberately written in Brazilian Portuguese
(`Escritorio`, `Empresa`, `NotaFiscal`, `Destinatario`) because it maps 1:1 onto the legal
vocabulary of the SEFAZ specification — translating it would put a lossy layer between the code
and the regulation it implements. Infrastructure concerns (namespaces, controllers, patterns)
use English.

---

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET Core 8, MediatR (CQRS), FluentValidation |
| Frontend | Blazor WebAssembly, MudBlazor 6.20 |
| Persistence | EF Core 8, PostgreSQL 16 |
| Auth | JWT (HMAC-SHA256), two-stage token exchange |
| Secrets at rest | ASP.NET Data Protection via a custom EF value converter |
| Tests | xUnit, SpecFlow (BDD), Testcontainers, FluentAssertions |
| Infra | Docker Compose; Render + Cloudflare Pages + Neon in production |

Projects are layered with dependencies pointing inward:

```
src/Domain          entities, enums, repository interfaces — no external dependencies
src/Application     MediatR commands/queries/handlers, DTOs, pipeline behaviors
src/Infrastructure  EF Core, repositories, SEFAZ/XML/certificate/tax services
src/API             controllers, middleware, background workers
src/WebUI           Blazor WASM client
```

---

## Architecture: the tenant hierarchy

```
Escritorio (accounting firm)  ← users belong HERE, not to a company
   └── Empresa (client company, holds CNPJ + digital certificate + SEFAZ config)
          └── NotaFiscal (invoice) ─── ItemNotaFiscal, EventoFiscal, Cliente, Produto
```

The important consequence: **a user is scoped to a firm, but every business operation is scoped
to a company.** One accountant legitimately acts on behalf of dozens of companies, so the
request-time tenant cannot be derived from the user record alone — it has to be selected, and
that selection has to be authorized. That is what the two-stage token exists for.

### How tenant isolation is actually enforced

Isolation rests on four mechanisms. They are described here as they exist in the code, including
where the guarantee is structural and where it depends on call-site discipline.

**1. The tenant is minted into the token, never accepted from the request.**

Login returns an access token that deliberately carries **no** `empresa_id` — only
`escritorio_id` — plus the list of companies the user may act for. To obtain a usable token the
client must call `POST /api/auth/selecionar-empresa`, and that handler is the single place in the
system where `empresa_id` is ever put into a token
([`AuthCommandHandlers.cs`](src/Application/Commands/Auth/AuthCommandHandlers.cs)):

```csharp
var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
if (empresa == null || empresa.EscritorioId != usuario.EscritorioId) return null;

return _tokenService.GerarAccessToken(usuario.Id, usuario.Email, usuario.Role,
                                      usuario.EscritorioId, empresa.Id);
```

Cross-firm selection fails at the moment of minting. A user can never hold a token for a company
outside their own firm, so no downstream code has to defend against a forged one — the token is
HMAC-SHA256 signed and the client cannot alter the claim.

**2. Controllers read the tenant from the claim, never from the payload.**

`BaseApiController` ([`BaseApiController.cs`](src/API/Controllers/BaseApiController.cs)) exposes
the tenant as a property sourced exclusively from the validated JWT:

```csharp
protected Guid EscritorioId => Guid.Parse(User.FindFirstValue("escritorio_id")!);
protected Guid EmpresaId    => Guid.Parse(User.FindFirstValue("empresa_id")!);
```

Every controller action passes `EmpresaId` into the MediatR command or query itself
(`new GetNotasQuery(EmpresaId, pagina, tamanhoPagina)`). No endpoint takes a company id from the
route, query string or body, which removes the whole class of IDOR bugs where a client
substitutes someone else's tenant id.

**3. Collection reads are filtered in the repository.**

Every repository method that returns a set requires an `empresaId` argument and applies it as a
predicate ([`Repositories.cs`](src/Infrastructure/Repositories/Repositories.cs)):

```csharp
.Where(n => n.EmpresaId == empresaId)     // notas fiscais
.Where(p => p.EmpresaId == empresaId && (!apenasAtivos || p.Ativo))   // produtos
.Where(c => c.EmpresaId == empresaId && (!apenasAtivos || c.Ativo))   // clientes
```

Firm-scoped listings (companies, users) go through `GetByEscritorioAsync` and filter on
`EscritorioId` the same way.

**4. Single-resource reads are re-checked by the caller — a deliberate, documented trade-off.**

`GetByIdAsync(Guid id)` is *not* tenant-filtered. Fetching by primary key returns the entity
regardless of owner, and the caller compares the owner against the claim before doing anything
with it:

```csharp
// NotaFiscalController.cs
if (nota == null || nota.EmpresaId != EmpresaId) return NotFound();

// CancelarNFeCommandHandler.cs
if (nota.EmpresaId != request.EmpresaId) return new CancelarNFeResult(false, "Acesso negado.");
```

This check is applied at every current call site (16 of them across controllers, query handlers
and command handlers), and the response is `404 NotFound` rather than `403 Forbidden` so the API
does not confirm that an id exists in another tenant.

Being explicit about the weakness: **this layer is convention, not a compiler- or
database-enforced invariant.** A future handler that calls `GetByIdAsync` and forgets the
comparison would leak across tenants, and nothing would fail to compile. EF Core global query
filters are already wired up in `NfeDbContext.OnModelCreating`, but only for soft-delete
(`HasQueryFilter(e => !e.IsDeleted)`) — not for tenancy, because the DbContext is registered per
request without an ambient tenant, so a tenant filter would have to be a scoped-service closure.
Promoting the check into a global query filter is the obvious hardening step and the reason the
isolation tests below assert behaviour end-to-end rather than trusting the convention.

**Verification.** Isolation is covered at two levels, both against a real PostgreSQL instance:
`tests/NfeSaas.Tests.Integration/Handlers/MultiTenantIsolationTests.cs` seeds two firms and
asserts that cross-firm company selection yields no token and that company/user listings return
only own-firm rows; `tests/NfeSaas.Tests.BDD/Features/IsolamentoMultiTenant.feature` asserts the
same three properties through the HTTP surface.

### Related invariants

- **Subscription gate.** `Escritorio.PodeAcessar()` is the single gate consulted by both
  `LoginCommandHandler` and `SelecionarEmpresaCommandHandler`, so an expired trial or suspended
  firm cannot obtain a company-scoped token by any path. Failures return typed codes
  (`TrialExpirado` → HTTP 402, `EscritorioSuspenso` → 403) that the UI renders distinctly.
- **Fiscal immutability.** An authorized invoice is legally frozen. An EF `SaveChanges`
  interceptor rejects mutations to fiscal fields on authorized notes; non-fiscal metadata such
  as `EmailEnviadoEm` is explicitly allowed. Covered by
  `FiscalImmutabilityInterceptorTests` and `NotaFiscalImutabilidadeTests`.
- **Secrets at rest.** `Empresa.CertificadoSenha` (digital certificate password) and
  `Empresa.CscToken` (NFC-e CSC) pass through `EncryptedStringConverter`, which wraps ASP.NET
  Data Protection and tags ciphertext with an `enc:v1:` prefix so legacy plaintext stays
  readable and is re-encrypted on next write. `EncryptedSecretsTests` reads the raw columns with
  ADO.NET to prove the values are not stored in the clear.
- **Domain integrity.** Entities have `protected` constructors plus a static `Criar(...)` factory
  and `private set` properties, so state changes only happen through methods that can enforce
  invariants.

---

## Tests

62 files under `tests/`, 44 of which contain test cases, across three projects:

| Project | What it covers | How it runs |
|---|---|---|
| `NfeSaas.Tests.Unit` | Domain invariants (entity factories, state transitions, fiscal immutability, trial/plan calculation) and isolated services: tax computation (ICMS, ICMS-ST, PIS, COFINS), CNPJ/IE/NCM/CFOP validators, XML generation, XSD validation, NFC-e QR code. Handlers are tested against in-memory fakes. | In process, no dependencies. **559 tests, ~1s.** |
| `NfeSaas.Tests.Integration` | What only a real database can prove: multi-tenant isolation, the fiscal-immutability `SaveChanges` interceptor, at-rest encryption verified by reading raw columns, and login/subscription gating. Uses `WebApplicationFactory` over the real DI graph. | Spins up PostgreSQL 16 via **Testcontainers** — needs Docker, not Docker Compose. |
| `NfeSaas.Tests.BDD` | Executable specification of the three user-visible guarantees: authentication (4 scenarios), company management (3), multi-tenant isolation (3). Gherkin, driven through the HTTP surface. | **SpecFlow** + Testcontainers, same fixture strategy. |

```bash
dotnet test tests/NfeSaas.Tests.Unit          # fast, no Docker
dotnet test tests/NfeSaas.Tests.Integration   # requires Docker
dotnet test tests/NfeSaas.Tests.BDD           # requires Docker
dotnet test                                   # everything
```

The integration and BDD fixtures inject `Jwt:Secret` and the connection string via `UseSetting`,
because `Program.cs` fails fast at boot if either is missing or still a placeholder.

---

## Running locally

Requires Docker and the .NET 8 SDK.

```bash
git clone <repo> && cd nfe-saas
cp .env.example .env
```

Fill in `.env` — the app refuses to boot with placeholder values:

```bash
openssl rand -base64 48   # → JWT_SECRET        (min. 32 chars)
openssl rand -base64 24   # → POSTGRES_PASSWORD
```

Then:

```bash
./restart.sh              # build, migrate, seed if empty, wait for /health
```

Or manually:

```bash
docker compose up -d --build
docker compose logs -f api
```

| Service | URL |
|---|---|
| API | http://localhost:5001 |
| Swagger | http://localhost:5001/swagger |
| WebUI | http://localhost:5002 |
| Health | http://localhost:5001/health |

Demo credentials from the seed: `admin@nfesaas.com.br` / `Admin@123`.

**Migrations.** The API container ships without the EF SDK, so migrations are applied as a
generated idempotent script rather than `dotnet ef database update` (`restart.sh` automates this):

```bash
dotnet ef migrations script --idempotent -o migration.sql \
  --project src/Infrastructure --startup-project src/API
docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -f /tmp/migration.sql
```

**SEFAZ.** `Sefaz:UseRealWebservice` defaults to `false`, so a local run talks to a stub and
needs no digital certificate. Real transmission — authorization, cancellation, correction
letters, voiding, recipient acknowledgement — is implemented against the actual SOAP endpoints
(`RecepcaoEvento4`, `NFeInutilizacao4`, `NFeConsultaProtocolo4`) with per-state URLs and
SVC-AN/SVC-RS contingency mapping. Always use `AmbienteSefaz.Homologacao` when testing.

---

## AI-assisted development

This codebase is written with AI agents (Claude Code) in the loop, and the repository is set up
so that the agent works from written constraints rather than from whatever it infers by reading
a few files.

**[`CLAUDE.md`](CLAUDE.md) is the operating contract.** It sits at the repo root and is loaded
into every agent session. It is not a description of the code — it is the set of rules that are
expensive to rediscover and costly to get wrong, for example:

- `DateTime.UtcNow` everywhere except Blazor UI, where local time is intended.
- Never interpolate `{decimal:F2}` into SEFAZ XML — under a pt-BR locale that emits a comma and
  the schema rejects the document. Use the `F2`/`F4` `InvariantCulture` helpers instead. This is
  a real bug that shipped once; the rule exists so it cannot ship twice.
- Every query filters by `EmpresaId` from the JWT claim; repositories never return other
  tenants' rows.
- Business logic goes in handlers, not controllers; entities mutate only through their own
  methods.

The pattern that makes this work: **each rule encodes a defect or a decision, not a style
preference.** Generic instructions ("write clean code") change nothing; a rule naming the exact
failure mode and the exact replacement is one an agent can apply reliably.

**`docs/` carries the specifications.** [`docs/README.md`](docs/README.md) holds the fiscal
domain rules — tax regimes, supported fiscal events, contingency behaviour, the API surface;
[`docs/UX_AUTOMACAO_FISCAL.md`](docs/UX_AUTOMACAO_FISCAL.md) the interaction design for the
fiscal automation flows; [`docs/deploy-producao.md`](docs/deploy-producao.md) the production
topology. Agents are pointed at these for the *what*, while `CLAUDE.md` governs the *how*, which
keeps invented fiscal behaviour out of the code — the domain is regulated and cannot be guessed.

**Human review gate.** Nothing merges on an agent's say-so:

1. **Tests must pass**, and the suite is deliberately weighted toward the parts a reviewer
   cannot verify by reading a diff — tenant isolation, at-rest encryption, and fiscal
   immutability are integration tests against a real database, not unit tests against mocks.
2. **Domain and security changes are read line by line by me.** Anything touching tenant
   scoping, the JWT flow, the immutability interceptor, or XML sent to SEFAZ gets manual review
   regardless of test results, because a passing suite proves the tested properties hold, not
   that the right properties were tested.
3. **Every commit is authored by a human** who has read the diff. Agent output is a proposal.

The honest summary: AI moves quickly through boilerplate — handlers, DTOs, EF configuration,
test scaffolding — and is genuinely useful for exhaustive validator and tax-calculation test
cases. It is not trusted with the parts where being wrong is expensive, and the repository is
structured to keep that boundary explicit rather than implicit.

---

## Status

Working pilot, not a mature product. Known limitations, stated plainly:

- Tenant isolation on single-resource reads is a call-site convention rather than a global query
  filter (see above) — the intended next hardening step.
- Plan activation is a direct admin endpoint; a payment-gateway webhook is what production needs.
- Production runs on free tiers (Render sleeps on inactivity, Neon autosuspends), fine for a
  pilot and not for load.
- SEFAZ per-state URLs were verified against official sources on 2026-08-18 and need periodic
  revalidation.
