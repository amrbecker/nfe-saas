using System.Net;

namespace NfeSaas.Tests.BDD.Support;

public class ScenarioState
{
    public HttpResponseMessage? LastResponse { get; set; }
    public string? CurrentToken { get; set; }
    public Guid? CurrentEscritorioId { get; set; }
    public Guid? CurrentEmpresaId { get; set; }
    public Dictionary<string, string> Tokens { get; } = new();
    public Dictionary<string, Guid> EmpresaIds { get; } = new();
    public Dictionary<string, Guid> EscritorioIds { get; } = new();
}
