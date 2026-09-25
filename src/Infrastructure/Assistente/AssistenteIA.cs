using NfeSaas.Application.Assistente;

namespace NfeSaas.Infrastructure.Assistente;

// STUB — implementado pelo pacote P3 (Microsoft.Extensions.AI + cliente compatível com OpenAI).
public class AssistenteIA : IAssistenteIA
{
    public bool EstaHabilitado(RotaIa rota) => false;
    public string Modelo(RotaIa rota) => "desabilitado";
    public Task<RespostaIa> CompletarAsync(RotaIa rota, IReadOnlyList<MensagemIa> mensagens, OpcoesIa opcoes, CancellationToken ct = default)
        => throw new InvalidOperationException("IA não configurada.");
    public decimal CustoEstimadoUsd(RotaIa rota, UsoIa uso) => 0m;
}
