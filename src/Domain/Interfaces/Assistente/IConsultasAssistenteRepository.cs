using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Interfaces.Assistente;

public record ItemResumoAssistente(int NumeroItem, string CodigoProduto, string Descricao, string Ncm, string? Cest, string Cfop,
    int CstIcms, int? Csosn, string UnidadeComercial);

public record NotaResumoAssistente(Guid Id, Guid EmpresaId, TipoNota Tipo, int Numero, string? DestinatarioCpfCnpj,
    string? DestinatarioUf, SituacaoNota Situacao, AmbienteSefaz Ambiente, DateTime DataEmissao, string? MotivoRejeicao,
    IReadOnlyList<ItemResumoAssistente> Itens);

public record EmpresaSaudeAssistente(Guid EmpresaId, Guid EscritorioId, string Nome, DateTime CriadaEm, DateTime? CertificadoValidade,
    bool TemCertificado, bool ConfiguracaoConcluida, AmbienteSefaz Ambiente);

/// <summary>Consultas de leitura (sem rastreamento) usadas pelas automações, pelo CS proativo e pela saúde das contas.</summary>
public interface IConsultasAssistenteRepository
{
    Task<List<NotaResumoAssistente>> NotasAsync(Guid empresaId, DateTime desde, SituacaoNota? situacao, int limite,
        CancellationToken ct = default);
    Task<List<EmpresaSaudeAssistente>> EmpresasAtivasAsync(CancellationToken ct = default);
    Task<List<Escritorio>> EscritoriosAtivosAsync(CancellationToken ct = default);
    Task<List<string>> EmailsAdminsAsync(Guid escritorioId, CancellationToken ct = default);
    Task<bool> TeveNotaAutorizadaAsync(Guid empresaId, AmbienteSefaz? ambiente, CancellationToken ct = default);
    Task<HashSet<string>> CodigosProdutosExistentesAsync(Guid empresaId, IEnumerable<string> codigos, CancellationToken ct = default);
}
