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

    public DateTime DataEmissao { get; private set; } = DateTime.Now;

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
        _itens.Add(item);
        RecalcularTotais();
    }

    public void SetTransporte(ModalidadeFrete modalidade, string? cnpj = null, string? razaoSocial = null,
        decimal frete = 0, decimal seguro = 0)
    {
        ModalidadeFrete = modalidade;
        TransportadoraCpfCnpj = cnpj;
        TransportadoraRazaoSocial = razaoSocial;
        TotalFrete = frete;
        TotalSeguro = seguro;
        RecalcularTotais();
    }

    public void SetPagamento(string formaPagamento, decimal valor)
    {
        FormaPagemento = formaPagamento;
        ValorPagamento = valor;
    }

    public void SetInformacoesAdicionais(string? info) => InformacoesAdicionais = info;

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
        XmlCancelamento = xmlCancelamento;
        Situacao = SituacaoNota.Cancelada;
        SetUpdated();
    }

    public void SetXmlEnvio(string xml) => XmlEnvio = xml;

    private void RecalcularTotais()
    {
        TotalProdutos = _itens.Sum(i => i.Quantidade * i.ValorUnitario);
        TotalDesconto = _itens.Sum(i => i.ValorDesconto);
        TotalIcms = _itens.Sum(i => i.ValorIcms);
        TotalIcmsSt = _itens.Sum(i => i.ValorIcmsSt) ?? 0m;
        TotalPis = _itens.Sum(i => i.ValorPis);
        TotalCofins = _itens.Sum(i => i.ValorCofins);
        TotalNota = TotalProdutos - TotalDesconto + TotalIcmsSt + TotalFrete + TotalSeguro + TotalOutrasDespesas;
    }
}
