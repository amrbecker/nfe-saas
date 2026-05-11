using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public enum IndicadorIeDestinatario
{
    Contribuinte = 1,
    IsentoIcms = 2,
    NaoContribuinte = 9
}

public class Cliente : BaseEntity
{
    public Guid EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public TipoPessoa TipoPessoa { get; private set; }
    public string? CpfCnpj { get; private set; }
    public string RazaoSocial { get; private set; } = null!;
    public string? NomeFantasia { get; private set; }
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public string Logradouro { get; private set; } = null!;
    public string Numero { get; private set; } = null!;
    public string? Complemento { get; private set; }
    public string Bairro { get; private set; } = null!;
    public string Cidade { get; private set; } = null!;
    public string Uf { get; private set; } = null!;
    public string Cep { get; private set; } = null!;
    public string CodigoMunicipio { get; private set; } = null!;
    public string? InscricaoEstadual { get; private set; }
    public IndicadorIeDestinatario IndicadorIe { get; private set; } = IndicadorIeDestinatario.NaoContribuinte;
    public bool Ativo { get; private set; } = true;

    protected Cliente() { }

    public static Cliente Criar(
        Guid empresaId, TipoPessoa tipoPessoa, string? cpfCnpj,
        string razaoSocial, string? nomeFantasia, string? email, string? telefone,
        string logradouro, string numero, string? complemento, string bairro, string cidade,
        string uf, string cep, string codigoMunicipio,
        string? inscricaoEstadual, IndicadorIeDestinatario indicadorIe)
    {
        return new Cliente
        {
            EmpresaId = empresaId,
            TipoPessoa = tipoPessoa,
            CpfCnpj = cpfCnpj,
            RazaoSocial = razaoSocial,
            NomeFantasia = nomeFantasia,
            Email = email,
            Telefone = telefone,
            Logradouro = logradouro,
            Numero = numero,
            Complemento = complemento,
            Bairro = bairro,
            Cidade = cidade,
            Uf = uf,
            Cep = cep,
            CodigoMunicipio = codigoMunicipio,
            InscricaoEstadual = inscricaoEstadual,
            IndicadorIe = indicadorIe
        };
    }

    public void Atualizar(
        TipoPessoa tipoPessoa, string? cpfCnpj,
        string razaoSocial, string? nomeFantasia, string? email, string? telefone,
        string logradouro, string numero, string? complemento, string bairro, string cidade,
        string uf, string cep, string codigoMunicipio,
        string? inscricaoEstadual, IndicadorIeDestinatario indicadorIe)
    {
        TipoPessoa = tipoPessoa;
        CpfCnpj = cpfCnpj;
        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        Email = email;
        Telefone = telefone;
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cidade = cidade;
        Uf = uf;
        Cep = cep;
        CodigoMunicipio = codigoMunicipio;
        InscricaoEstadual = inscricaoEstadual;
        IndicadorIe = indicadorIe;
        SetUpdated();
    }

    public void Desativar() { Ativo = false; SetUpdated(); }
    public void Ativar()    { Ativo = true;  SetUpdated(); }
}
