using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class ItemNotaFiscal : BaseEntity
{
    public Guid NotaFiscalId { get; private set; }
    public NotaFiscal NotaFiscal { get; private set; } = null!;

    public int NumeroItem { get; private set; }
    public string CodigoProduto { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string? CodigoEan { get; private set; }
    public string Ncm { get; private set; } = null!;
    public string? Cest { get; private set; }
    public string Cfop { get; private set; } = null!;
    public string UnidadeComercial { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal ValorTotal { get; private set; }
    public decimal ValorDesconto { get; private set; }

    // ICMS
    public OrigemMercadoria OrigemMercadoria { get; private set; }
    public CstIcms CstIcms { get; private set; }
    public CsosnIcms? CsosnIcms { get; private set; }   // Simples Nacional — quando setado, prevalece sobre CstIcms
    public decimal BaseCalculoIcms { get; private set; }
    public decimal AliquotaIcms { get; private set; }
    public decimal ValorIcms { get; private set; }
    public decimal? BaseCalculoIcmsReducao { get; private set; }
    public decimal? ValorIcmsSt { get; private set; } = 0;
    public decimal? BaseCalculoIcmsSt { get; private set; }
    public decimal? AliquotaIcmsSt { get; private set; }

    // PIS
    public CstPisCofins CstPis { get; private set; }
    public decimal BaseCalculoPis { get; private set; }
    public decimal AliquotaPis { get; private set; }
    public decimal ValorPis { get; private set; }

    // COFINS
    public CstPisCofins CstCofins { get; private set; }
    public decimal BaseCalculoCofins { get; private set; }
    public decimal AliquotaCofins { get; private set; }
    public decimal ValorCofins { get; private set; }

    // IPI (opcional)
    public string? CstIpi { get; private set; }
    public decimal? BaseCalculoIpi { get; private set; }
    public decimal? AliquotaIpi { get; private set; }
    public decimal? ValorIpi { get; private set; }

    // FCP — Fundo de Combate à Pobreza (% adicional sobre ICMS em algumas UFs)
    public decimal? BaseCalculoFcp { get; private set; }
    public decimal? AliquotaFcp { get; private set; }
    public decimal? ValorFcp { get; private set; }

    // DIFAL — Diferencial de Alíquota (operação interestadual a consumidor final não contribuinte)
    public decimal? BaseCalculoDifal { get; private set; }
    public decimal? AliquotaInternaUfDestino { get; private set; }
    public decimal? AliquotaInterestadual { get; private set; }
    public decimal? ValorIcmsUfDestino { get; private set; }
    public decimal? ValorIcmsUfRemetente { get; private set; }

    protected ItemNotaFiscal() { }

    public static ItemNotaFiscal Criar(
        Guid notaFiscalId, int numeroItem, string codigoProduto, string descricao,
        string ncm, string cfop, string unidade, decimal quantidade, decimal valorUnitario,
        decimal desconto = 0)
    {
        var total = (quantidade * valorUnitario) - desconto;
        return new ItemNotaFiscal
        {
            NotaFiscalId = notaFiscalId,
            NumeroItem = numeroItem,
            CodigoProduto = codigoProduto,
            Descricao = descricao,
            Ncm = ncm,
            Cfop = cfop,
            UnidadeComercial = unidade,
            Quantidade = quantidade,
            ValorUnitario = valorUnitario,
            ValorDesconto = desconto,
            ValorTotal = total
        };
    }

    public void SetIcms(OrigemMercadoria origem, CstIcms cst, decimal baseCalculo, decimal aliquota)
    {
        OrigemMercadoria = origem;
        CstIcms = cst;
        CsosnIcms = null;
        BaseCalculoIcms = baseCalculo;
        AliquotaIcms = aliquota;
        ValorIcms = Math.Round(baseCalculo * (aliquota / 100), 2);
    }

    public void SetIcmsSimples(OrigemMercadoria origem, CsosnIcms csosn, decimal baseCalculo, decimal aliquota)
    {
        OrigemMercadoria = origem;
        CsosnIcms = csosn;
        BaseCalculoIcms = baseCalculo;
        AliquotaIcms = aliquota;
        // Apenas CSOSN 900 calcula ICMS próprio na nota. Demais (101, 102, 103, 300, 400, 500, 201, 202, 203) zeram.
        ValorIcms = csosn == NfeSaas.Domain.Enums.CsosnIcms.Outros
            ? Math.Round(baseCalculo * (aliquota / 100), 2)
            : 0;
    }

    public void SetIcmsSt(decimal baseCalculo, decimal aliquota)
    {
        BaseCalculoIcmsSt = baseCalculo;
        AliquotaIcmsSt = aliquota;
        ValorIcmsSt = Math.Round(baseCalculo * (aliquota / 100), 2);
    }

    public void SetPis(CstPisCofins cst, decimal baseCalculo, decimal aliquota)
    {
        CstPis = cst;
        BaseCalculoPis = baseCalculo;
        AliquotaPis = aliquota;
        ValorPis = Math.Round(baseCalculo * (aliquota / 100), 2);
    }

    public void SetCofins(CstPisCofins cst, decimal baseCalculo, decimal aliquota)
    {
        CstCofins = cst;
        BaseCalculoCofins = baseCalculo;
        AliquotaCofins = aliquota;
        ValorCofins = Math.Round(baseCalculo * (aliquota / 100), 2);
    }

    public void SetCodigoEan(string ean) => CodigoEan = ean;
    public void SetCest(string cest) => Cest = cest;

    public void SetIpi(string cst, decimal baseCalculo, decimal aliquota)
    {
        CstIpi = cst;
        BaseCalculoIpi = baseCalculo;
        AliquotaIpi = aliquota;
        ValorIpi = Math.Round(baseCalculo * (aliquota / 100m), 2);
    }

    public void SetFcp(decimal baseCalculo, decimal aliquota)
    {
        BaseCalculoFcp = baseCalculo;
        AliquotaFcp = aliquota;
        ValorFcp = Math.Round(baseCalculo * (aliquota / 100m), 2);
    }

    public void SetDifal(decimal baseCalculo, decimal aliquotaInternaUfDestino, decimal aliquotaInterestadual)
    {
        BaseCalculoDifal = baseCalculo;
        AliquotaInternaUfDestino = aliquotaInternaUfDestino;
        AliquotaInterestadual = aliquotaInterestadual;
        // EC 87/2015 — Convênio ICMS 93/2015: a partilha foi gradual (2016-2018) e em 2019+ vai 100% pra UF destino.
        var diferenca = Math.Max(0, aliquotaInternaUfDestino - aliquotaInterestadual);
        ValorIcmsUfDestino = Math.Round(baseCalculo * (diferenca / 100m), 2);
        ValorIcmsUfRemetente = 0; // Partilha 100% destino desde 2019
    }
}
