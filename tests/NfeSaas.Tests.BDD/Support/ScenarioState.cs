using System.Net;
using NfeSaas.Application.DTOs;

namespace NfeSaas.Tests.BDD.Support;

public class ScenarioState
{
    public HttpResponseMessage? LastResponse { get; set; }
    // Cache do corpo de LastResponse já desserializado — o conteúdo de HttpContent só pode ser
    // lido uma vez; múltiplos steps "Then" que inspecionam a mesma resposta (ex.: token válido
    // + lista de empresas) devem reaproveitar essa leitura em vez de reler o stream.
    public LoginResultDto? LastLoginResult { get; set; }
    public string? CurrentToken { get; set; }
    public Guid? CurrentEscritorioId { get; set; }
    public Guid? CurrentEmpresaId { get; set; }
    public Dictionary<string, string> Tokens { get; } = new();
    public Dictionary<string, Guid> EmpresaIds { get; } = new();
    public Dictionary<string, Guid> EscritorioIds { get; } = new();
}
