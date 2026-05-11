using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class NotaFiscal : BaseEntity
{
    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    // Identificação
    public TipoNota Tipo { get; private set; }
    public int Serie { get; private set; }
    public int Numero { get; private set; }
    public string? ChaveAcesso { get; private set; }
    public string? Protocolo { get; private set; }
    public DateTime? DataAutorizacao { get; private set; }
    public FinalidadeNota Finalidade { get; private set; }
    public TipoOperacao TipoOperacao { get; private set; }
    public AmbienteSefaz Ambiente { get; private set; }
    public SituacaoNota Situacao { get; private set; } = SituacaoNota.Rascunho;
    public TipoEmissao TipoEmissao { get; private set; } = TipoEmissao.Normal;

    // Destinatário
    public string? DestinatarioCpfCnpj { get; private set; }
    public string? DestinatarioRazaoSocial { get; private set; }
    public string? DestinatarioEmail { get; private set; }
    public string? DestinatarioLogradouro { get; private set; }
    public string? DestinatarioNumero { get; private set; }
    public string? DestinatarioComplemento { get; private set; }
    public string? DestinatarioBairro { get; private set; }
    public string? DestinatarioCidade { get; private set; }
    public string? DestinatarioUf { get; private set; }
    public string? DestinatarioCep { get; private set; }
    public string? DestinatarioCodigoMunicipio { get; private set; }
    public string? DestinatarioInscricaoEstadual { get; private set; }
    public TipoPessoa DestinatarioTipoPessoa { get; private set; }

    // Totais
    public decimal TotalProdutos { get; private set; }
    public decimal TotalDesconto { get; private set; }
    public decimal TotalIcms { get; private set; }
    public decimal TotalIcmsSt { get; private set; }
    public decimal TotalPis { get; private set; }
    public decimal TotalCofins { get; private set; }
    public decimal TotalIpi { get; private set; }
    public decimal TotalFcp { get; private set; }
    public decimal TotalIcmsUfDestino { get; private set; }   // DIFAL — parcela UF destino
    public decimal TotalIcmsUfRemetente { get; private set; } // DIFAL — parcela UF origem (zerada desde 2019)
    public decimal TotalFrete { get; private set; }
    public decimal TotalSeguro { get; private set; }
    public decimal TotalOutrasDespesas { get; private set; }
    public decimal TotalNota { get; private set; }

    // Transporte
    public ModalidadeFrete ModalidadeFrete { get; private set; }
    public string? TransportadoraCpfCnpj { get; private set; }
    public string? TransportadoraRazaoSocial { get; private set; }

    // Pagamento
    public string FormaPagemento { get; private set; } = "01"; // 01 = Dinheiro
    public decimal ValorPagamento { get; private set; }

    // XML e DANFE
    public string? XmlEnvio { get; private set; }
    public string? XmlRetorno { get; private set; }
    public string? XmlCancelamento { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public string? InformacoesAdicionais { get; private set; }

    public DateTime DataEmissao { get; private set; } = DateTime.UtcNow;
    public DateTime? DataCancelamento { get; private set; }

    // Período legal de retenção fiscal (5 anos a partir da autorização ou cancelamento — Lei nº 10.522/02 e CTN art. 173).
    private const int AnosRetencaoFiscal = 5;
    public DateTime? DataDescarteAutorizado
    {
        get
        {
            if (Situacao == SituacaoNota.Cancelada && DataCancelamento.HasValue)
                return DataCancelamento.Value.AddYears(AnosRetencaoFiscal);
            if (Situacao == SituacaoNota.Autorizada && DataAutorizacao.HasValue)
                return DataAutorizacao.Value.AddYears(AnosRetencaoFiscal);
            return null;
        }
    }
    public bool DentroPeriodoRetencao =>
        DataDescarteAutorizado.HasValue && DataDescarteAutorizado.Value > DateTime.UtcNow;
    private bool IsImutavel =>
        Situacao == SituacaoNota.Autorizada || Situacao == SituacaoNota.Cancelada;

    private void EnsureMutavel(string operacao)
    {
        if (IsImutavel)
            throw new InvalidOperationException(
                $"Não é permitido {operacao} em uma NFe {Situacao}. " +
                $"Documento fiscal autorizado é imutável conforme legislação.");
    }

    private readonly List<ItemNotaFiscal> _itens = new();
    public IReadOnlyCollection<ItemNotaFiscal> Itens => _itens.AsReadOnly();

    protected NotaFiscal() { }

    public static NotaFiscal Criar(
        Guid empresaId, TipoNota tipo, int serie, int numero,
        FinalidadeNota finalidade, TipoOperacao operacao, AmbienteSefaz ambiente)
    {
        return new NotaFiscal
        {
            EmpresaId = empresaId,
            Tipo = tipo,
            Serie = serie,
            Numero = numero,
            Finalidade = finalidade,
            TipoOperacao = operacao,
            Ambiente = ambiente
        };
    }

    public void SetDestinatario(
        string? cpfCnpj, string? razaoSocial, string? email, TipoPessoa tipoPessoa,
        string? logradouro, string? numero, string? bairro, string? cidade,
        string? uf, string? cep, string? codigoMunicipio, string? ie = null)
    {
        EnsureMutavel("alterar destinatário");
        DestinatarioCpfCnpj = cpfCnpj;
        DestinatarioRazaoSocial = razaoSocial;
        DestinatarioEmail = email;
        DestinatarioTipoPessoa = tipoPessoa;
        DestinatarioLogradouro = logradouro;
        DestinatarioNumero = numero;
        DestinatarioBairro = bairro;
        DestinatarioCidade = cidade;
        DestinatarioUf = uf;
        DestinatarioCep = cep;
        DestinatarioCodigoMunicipio = codigoMunicipio;
        DestinatarioInscricaoEstadual = ie;
    }

    public void AdicionarItem(ItemNotaFiscal item)
    {
        EnsureMutavel("adicionar item");
        _itens.Add(item);
        RecalcularTotais();
    }

    public void SetTransporte(ModalidadeFrete modalidade, string? cnpj = null, string? razaoSocial = null,
        decimal frete = 0, decimal seguro = 0)
    {
        EnsureMutavel("alterar transporte");
        ModalidadeFrete = modalidade;
        TransportadoraCpfCnpj = cnpj;
        TransportadoraRazaoSocial = razaoSocial;
        TotalFrete = frete;
        TotalSeguro = seguro;
        RecalcularTotais();
    }

    public void SetPagamento(string formaPagamento, decimal valor)
    {
        EnsureMutavel("alterar pagamento");
        FormaPagemento = formaPagamento;
        ValorPagamento = valor;
    }

    public void SetInformacoesAdicionais(string? info)
    {
        EnsureMutavel("alterar informações adicionais");
        InformacoesAdicionais = info;
    }

    public void Autorizar(string chaveAcesso, string protocolo, string xmlRetorno)
    {
        ChaveAcesso = chaveAcesso;
        Protocolo = protocolo;
        XmlRetorno = xmlRetorno;
        DataAutorizacao = DateTime.UtcNow;
        Situacao = SituacaoNota.Autorizada;
        SetUpdated();
    }

    public void MarcarEnviada(string xmlEnvio)
    {
        XmlEnvio = xmlEnvio;
        Situacao = SituacaoNota.Enviada;
        SetUpdated();
    }

    public void Rejeitar(string motivo)
    {
        MotivoRejeicao = motivo;
        Situacao = SituacaoNota.Rejeitada;
        SetUpdated();
    }

    public void Cancelar(string xmlCancelamento)
    {
        if (Situacao != SituacaoNota.Autorizada)
            throw new InvalidOperationException("Apenas notas autorizadas podem ser canceladas.");
        XmlCancelamento = xmlCancelamento;
        Situacao = SituacaoNota.Cancelada;
        DataCancelamento = DateTime.UtcNow;
        SetUpdated();
    }

    public void SetXmlEnvio(string xml)
    {
        EnsureMutavel("alterar XML de envio");
        XmlEnvio = xml;
    }

    public void MarcarContingencia(TipoEmissao tipo)
    {
        EnsureMutavel("marcar contingência");
        TipoEmissao = tipo;
        SetUpdated();
    }

    public override void Delete()
    {
        // Nota fiscal autorizada/cancelada está sob retenção legal de 5 anos.
        if (DentroPeriodoRetencao)
            throw new InvalidOperationException(
                $"Esta nota fiscal está sob retenção fiscal até {DataDescarteAutorizado:dd/MM/yyyy}. " +
                "Documentos fiscais autorizados ou cancelados não podem ser excluídos antes desse prazo.");
        base.Delete();
    }

    private void RecalcularTotais()
    {
        TotalProdutos = _itens.Sum(i => i.Quantidade * i.ValorUnitario);
        TotalDesconto = _itens.Sum(i => i.ValorDesconto);
        TotalIcms = _itens.Sum(i => i.ValorIcms);
        TotalIcmsSt = _itens.Sum(i => i.ValorIcmsSt) ?? 0m;
        TotalPis = _itens.Sum(i => i.ValorPis);
        TotalCofins = _itens.Sum(i => i.ValorCofins);
        TotalIpi = _itens.Sum(i => i.ValorIpi ?? 0m);
        TotalFcp = _itens.Sum(i => i.ValorFcp ?? 0m);
        TotalIcmsUfDestino = _itens.Sum(i => i.ValorIcmsUfDestino ?? 0m);
        TotalIcmsUfRemetente = _itens.Sum(i => i.ValorIcmsUfRemetente ?? 0m);
        // vNF inclui IPI e FCP (FCP-ST inclusive). DIFAL é informativo e não soma em vNF.
        TotalNota = TotalProdutos - TotalDesconto + TotalIcmsSt + TotalFrete + TotalSeguro
                  + TotalOutrasDespesas + TotalIpi + TotalFcp;
    }
}
