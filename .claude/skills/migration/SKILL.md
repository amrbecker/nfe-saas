---
name: migration
description: Cria uma migration EF Core e aplica no Postgres local pelo fluxo idempotente do projeto (script SQL + docker cp + psql), nunca com dotnet ef database update.
disable-model-invocation: true
argument-hint: "<NomeDaMigration em PascalCase>"
---

# Migration: $ARGUMENTS

**Proibido:** `dotnet ef database update` (CLAUDE.md). O container da API não tem SDK do EF.

1. Confirme que a mudança de entidade/configuração já está feita e compila: `dotnet build src/API`.
2. Gere:
   ```bash
   dotnet ef migrations add $ARGUMENTS --project src/Infrastructure --startup-project src/API
   ```
3. **Revise** o `.cs` gerado em `src/Infrastructure/Migrations/`: colunas `NOT NULL` sem default em tabela com dados,
   renomes que viraram drop+add (perda de dados), índices de `EmpresaId` em tabelas novas de tenant.
   Os `.Designer.cs`/`ModelSnapshot` não devem ser editados à mão (há hook bloqueando).
4. Aplique no banco local:
   ```bash
   dotnet ef migrations script --idempotent -o migration.sql --project src/Infrastructure --startup-project src/API
   docker cp migration.sql nfesaas_postgres:/tmp/migration.sql
   docker exec nfesaas_postgres psql -U nfesaas -d nfesaas -v ON_ERROR_STOP=1 -f /tmp/migration.sql
   rm migration.sql
   ```
   (Se o container não estiver de pé: `./restart.sh --no-ide --no-seed` faz build + migrations.)
5. Rode `dotnet test tests/NfeSaas.Tests.Integration` (Testcontainers aplica as migrations do zero).
6. Lembre o usuário: em produção a migration roda sozinha no boot do Render (`Database__MigrateOnStartup=true`). Se for destrutiva, avise para tirar snapshot do Neon antes do deploy (`docs/deploy-producao.md`).
