using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class EventoFiscal : BaseEntity
{
    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;
    public Guid? UsuarioId { get; private set; }

    public TipoEventoFiscal Tipo { get; private set; }
    public AmbienteSefaz Ambiente { get; private set; }

    // Para CC-e e manifestações: chave da NF-e alvo
    public string? ChaveAcesso { get; private set; }

    // Apenas CC-e: sequencial (1-20) — uma chave pode ter até 20 CC-e
    public int? SequencialCce { get; private set; }

    // Apenas Inutilização: range
    public int? AnoInutilizacao { get; private set; }
    public TipoNota? TipoNotaInutilizacao { get; private set; }
    public int? SerieInutilizacao { get; private set; }
    public int? NumeroInicialInutilizacao { get; private set; }
    public int? NumeroFinalInutilizacao { get; private set; }

    // Conteúdo do evento (texto da correção, justificativa de inutilização, etc.)
    public string Justificativa { get; private set; } = null!;

    // Resultado SEFAZ
    public SituacaoEventoFiscal Situacao { get; private set; } = SituacaoEventoFiscal.Registrado;
    public string? Protocolo { get; private set; }
    public string? XmlEvento { get; private set; }
    public string? XmlRetorno { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public DateTime DataEvento { get; private set; } = DateTime.UtcNow;
    public DateTime? DataRetorno { get; private set; }

    protected EventoFiscal() { }

    public static EventoFiscal CriarCce(
        Guid empresaId, Guid? usuarioId, AmbienteSefaz ambiente,
        string chaveAcesso, int sequencial, string correcao)
    {
        return new EventoFiscal
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Ambiente = ambiente,
            Tipo = TipoEventoFiscal.CartaCorrecao,
            ChaveAcesso = chaveAcesso,
            SequencialCce = sequencial,
            Justificativa = correcao
        };
    }

    public static EventoFiscal CriarInutilizacao(
        Guid empresaId, Guid? usuarioId, AmbienteSefaz ambiente,
        int ano, TipoNota tipoNota, int serie, int numIni, int numFin, string justificativa)
    {
        return new EventoFiscal
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Ambiente = ambiente,
            Tipo = TipoEventoFiscal.Inutilizacao,
            AnoInutilizacao = ano,
            TipoNotaInutilizacao = tipoNota,
            SerieInutilizacao = serie,
            NumeroInicialInutilizacao = numIni,
            NumeroFinalInutilizacao = numFin,
            Justificativa = justificativa
        };
    }

    public static EventoFiscal CriarManifestacao(
        Guid empresaId, Guid? usuarioId, AmbienteSefaz ambiente,
        TipoEventoFiscal tipoManifestacao, string chaveAcesso, string justificativa)
    {
        return new EventoFiscal
        {
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Ambiente = ambiente,
            Tipo = tipoManifestacao,
            ChaveAcesso = chaveAcesso,
            Justificativa = justificativa
        };
    }

    public void RegistrarEnvio(string xmlEvento)
    {
        XmlEvento = xmlEvento;
        SetUpdated();
    }

    public void Aceitar(string protocolo, string? xmlRetorno)
    {
        Situacao = SituacaoEventoFiscal.Aceito;
        Protocolo = protocolo;
        XmlRetorno = xmlRetorno;
        DataRetorno = DateTime.UtcNow;
        MotivoRejeicao = null;
        SetUpdated();
    }

    public void Rejeitar(string motivo, string? xmlRetorno = null)
    {
        Situacao = SituacaoEventoFiscal.Rejeitado;
        MotivoRejeicao = motivo;
        XmlRetorno = xmlRetorno;
        DataRetorno = DateTime.UtcNow;
        SetUpdated();
    }
}
