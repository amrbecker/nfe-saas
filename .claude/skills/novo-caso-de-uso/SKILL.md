---
name: novo-caso-de-uso
description: Cria um novo caso de uso no NfeSaas seguindo o padrão do projeto — Command/Query record MediatR + handler, método de repositório, endpoint fino no controller e teste unitário — com checklist de multi-tenant, UtcNow e PersonalizacaoService. Use ao adicionar qualquer funcionalidade nova de backend.
argument-hint: "<descrição do caso de uso, ex.: 'duplicar nota fiscal como rascunho'>"
---

# Novo caso de uso: $ARGUMENTS

Siga o padrão já existente — **leia um exemplo antes de escrever**:
- Command + handler: `src/Application/Commands/CancelarNFe/CancelarNFeCommandHandler.cs`
- Query: `src/Application/Queries/NotaFiscalQueries.cs`
- Controller: `src/API/Controllers/NotaFiscalController.cs` (herda `BaseApiController`)
- Teste: `tests/NfeSaas.Tests.Unit/Application/CancelarNFeHandlerTests.cs` (xUnit + Moq + FluentAssertions)

## Passos (TDD — teste primeiro)

1. **Decida Command vs Query.** Escrita → `src/Application/Commands/<Nome>/<Nome>CommandHandler.cs`; leitura → arquivo em `src/Application/Queries/`.
   - `record` com `EmpresaId` (e `UsuarioId` quando houver auditoria) como parâmetros; result `record` com `Sucesso`/`MensagemErro` ou DTO.
   - Nomes de domínio em PT-BR.
2. **Teste unitário primeiro** em `tests/NfeSaas.Tests.Unit/Application/<Nome>HandlerTests.cs`, incluindo obrigatoriamente o caso
   *"recurso de outra empresa → Acesso negado / não encontrado"*. Rode e veja falhar:
   `dotnet test tests/NfeSaas.Tests.Unit --filter FullyQualifiedName~<Nome>`
3. **Domínio**: mutação só por método da entidade (`private set`, factory `Criar(...)`). Regra de negócio fica na entidade/handler, nunca no controller.
4. **Repositório** (se preciso): interface em `src/Domain/Interfaces/IRepositories.cs`, implementação em `src/Infrastructure/Repositories/`.
   Toda assinatura termina em `CancellationToken ct = default` e filtra por `EmpresaId`.
5. **Handler**: carregue o recurso e compare `recurso.EmpresaId != request.EmpresaId` → negar. Use `DateTime.UtcNow`. Audite via `IAuditService` se for ação fiscal.
6. **Controller**: só `Mediator.Send(new XCommand(..., EmpresaId, ..., UserId))` — `EmpresaId`/`UserId` vêm de `BaseApiController` (claim JWT), **nunca do body/rota**. `[Authorize(Roles = "Admin")]` se for administrativo.
7. **XML/SEFAZ**: decimais só com `F2(...)`/`F4(...)`; testes sempre em `AmbienteSefaz.Homologacao`.
8. **Banco**: campo novo → use `/migration <Nome>`.
9. **WebUI** (se houver tela): serviço em `src/WebUI/Services/`, página MudBlazor 6.20, e avalie gatear pela flag semântica do `PersonalizacaoService`.

## Checklist final (responda cada item explicitamente)
- [ ] `EmpresaId` vem do JWT e o handler verifica posse do recurso
- [ ] Teste de isolamento entre empresas existe e passa
- [ ] Nenhum `DateTime.Now` / `{x:F2}` em XML
- [ ] Controller sem lógica de negócio
- [ ] `dotnet build NfeSaas.sln` e `dotnet test tests/NfeSaas.Tests.Unit` verdes
- [ ] Se tocou em dados de tenant ou na Ori: rodar o agente `tenant-isolation-reviewer`
