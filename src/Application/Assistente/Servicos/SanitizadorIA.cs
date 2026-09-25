namespace NfeSaas.Application.Assistente.Servicos;

// STUB — implementado pelo pacote P3.
public class SanitizadorIA : ISanitizadorIA
{
    public ResultadoSanitizacao Sanitizar(string? texto, IDictionary<string, string>? mapa = null) => throw new NotImplementedException();
    public string Reidratar(string texto, IReadOnlyDictionary<string, string> substituicoes) => throw new NotImplementedException();
    public AtributosPessoa DescreverPessoa(string? cpfCnpj, string? uf, string? inscricaoEstadual, int? indicadorIe = null) => throw new NotImplementedException();
}
