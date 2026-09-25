using NfeSaas.Application.DTOs.Assistente;

namespace NfeSaas.Application.Assistente.Servicos;

// STUB — implementado pelo pacote P3.
public class CotaAssistente : ICotaAssistente
{
    public Task<StatusCotaDto> ObterAsync(Guid usuarioId, Guid escritorioId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task RegistrarAsync(Guid usuarioId, Guid escritorioId, UsoIa uso, decimal custoUsd, CancellationToken ct = default) => throw new NotImplementedException();
}
