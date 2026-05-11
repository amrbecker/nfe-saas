using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Infrastructure.Data.Interceptors;

/// <summary>
/// Defesa de imutabilidade fiscal no nível EF: bloqueia alterações em campos
/// "imutáveis" de NotaFiscal autorizada/cancelada, mesmo que código de aplicação
/// tente forçar (ex: bypass do domínio via reflection ou contexto compartilhado).
///
/// Para Autorizada → permite apenas transição para Cancelada (Situacao + XmlCancelamento + DataCancelamento + UpdatedAt).
/// Para Cancelada → nada pode mudar.
/// </summary>
public class FiscalImmutabilityInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> _camposPermitidosNoCancelamento = new()
    {
        nameof(NotaFiscal.Situacao),
        nameof(NotaFiscal.XmlCancelamento),
        nameof(NotaFiscal.DataCancelamento),
        nameof(BaseEntityProps.UpdatedAt),
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Validate(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Validate(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Validate(DbContext? ctx)
    {
        if (ctx == null) return;

        foreach (var entry in ctx.ChangeTracker.Entries<NotaFiscal>())
        {
            if (entry.State != EntityState.Modified) continue;

            // Pega a Situacao ANTES da modificação
            var situacaoOriginal = (SituacaoNota)entry.Property(nameof(NotaFiscal.Situacao)).OriginalValue!;
            if (situacaoOriginal != SituacaoNota.Autorizada && situacaoOriginal != SituacaoNota.Cancelada)
                continue;

            var camposAlterados = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();

            // Cancelada → nada pode mudar
            if (situacaoOriginal == SituacaoNota.Cancelada)
            {
                throw new InvalidOperationException(
                    $"Tentativa de alterar nota fiscal cancelada (chave {entry.Entity.ChaveAcesso}). " +
                    $"Campos modificados: {string.Join(", ", camposAlterados)}. " +
                    "Documentos fiscais cancelados são imutáveis.");
            }

            // Autorizada → só pode ir pra Cancelada (campos restritos)
            if (situacaoOriginal == SituacaoNota.Autorizada)
            {
                var camposNaoPermitidos = camposAlterados
                    .Where(c => !_camposPermitidosNoCancelamento.Contains(c))
                    .ToList();

                if (camposNaoPermitidos.Count > 0)
                    throw new InvalidOperationException(
                        $"Tentativa de alterar campos imutáveis de NFe autorizada " +
                        $"(chave {entry.Entity.ChaveAcesso}): {string.Join(", ", camposNaoPermitidos)}. " +
                        "Apenas o cancelamento (Situação + XmlCancelamento) é permitido após autorização.");

                var novaSituacao = (SituacaoNota)entry.Property(nameof(NotaFiscal.Situacao)).CurrentValue!;
                if (novaSituacao != SituacaoNota.Cancelada)
                    throw new InvalidOperationException(
                        $"NFe autorizada (chave {entry.Entity.ChaveAcesso}) só pode transicionar para Cancelada. " +
                        $"Tentativa: Autorizada → {novaSituacao}.");
            }
        }
    }

    // Helper para nome dos campos da BaseEntity
    private static class BaseEntityProps
    {
        public const string UpdatedAt = nameof(NfeSaas.Domain.Common.BaseEntity.UpdatedAt);
    }
}
