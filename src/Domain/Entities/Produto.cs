using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class Produto : BaseEntity
{
    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public string Codigo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string Ncm { get; private set; } = null!;
    public string? Cest { get; private set; }
    public string CfopPadrao { get; private set; } = null!;
    public string UnidadeComercial { get; private set; } = null!;
    public OrigemMercadoria OrigemMercadoria { get; private set; }
    public decimal ValorUnitarioPadrao { get; private set; }
    public string? CodigoEan { get; private set; }
    public string? CodigoAnp { get; private set; }
    public bool Ativo { get; private set; } = true;

    protected Produto() { }

    public static Produto Criar(
        Guid empresaId, string codigo, string descricao, string ncm,
        string cfopPadrao, string unidadeComercial, OrigemMercadoria origem,
        decimal valorUnitarioPadrao,
        string? cest = null, string? codigoEan = null, string? codigoAnp = null)
    {
        return new Produto
        {
            EmpresaId = empresaId,
            Codigo = codigo,
            Descricao = descricao,
            Ncm = ncm,
            CfopPadrao = cfopPadrao,
            UnidadeComercial = unidadeComercial,
            OrigemMercadoria = origem,
            ValorUnitarioPadrao = valorUnitarioPadrao,
            Cest = cest,
            CodigoEan = codigoEan,
            CodigoAnp = codigoAnp
        };
    }

    public void Atualizar(
        string codigo, string descricao, string ncm,
        string cfopPadrao, string unidadeComercial, OrigemMercadoria origem,
        decimal valorUnitarioPadrao,
        string? cest, string? codigoEan, string? codigoAnp)
    {
        Codigo = codigo;
        Descricao = descricao;
        Ncm = ncm;
        CfopPadrao = cfopPadrao;
        UnidadeComercial = unidadeComercial;
        OrigemMercadoria = origem;
        ValorUnitarioPadrao = valorUnitarioPadrao;
        Cest = cest;
        CodigoEan = codigoEan;
        CodigoAnp = codigoAnp;
        SetUpdated();
    }

    public void Desativar() { Ativo = false; SetUpdated(); }
    public void Ativar()    { Ativo = true;  SetUpdated(); }
}
