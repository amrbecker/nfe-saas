using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

public class Empresa : BaseEntity
{
    public Guid EscritorioId { get; private set; }
    public Escritorio Escritorio { get; private set; } = null!;

    public string RazaoSocial { get; private set; } = null!;
    public string NomeFantasia { get; private set; } = null!;
    public string Cnpj { get; private set; } = null!;
    public string InscricaoEstadual { get; private set; } = null!;
    public string? InscricaoMunicipal { get; private set; }
    public string Logradouro { get; private set; } = null!;
    public string Numero { get; private set; } = null!;
    public string? Complemento { get; private set; }
    public string Bairro { get; private set; } = null!;
    public string Cidade { get; private set; } = null!;
    public string Uf { get; private set; } = null!;
    public string Cep { get; private set; } = null!;
    public string CodigoMunicipio { get; private set; } = null!;
    public string Telefone { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Cnae { get; private set; }
    public RegimeTributario RegimeTributario { get; private set; }
    public AmbienteSefaz AmbienteSefaz { get; private set; }
    public int UltimoNumeronFe { get; private set; }
    public int UltimoNumeronFCe { get; private set; }
    public int SerieNFe { get; private set; } = 1;
    public int SerieNFCe { get; private set; } = 1;
    public string? CaminhoLogotipo { get; private set; }
    public bool Ativo { get; private set; } = true;

    // Certificado A1
    public byte[]? CertificadoBytes { get; private set; }
    public string? CertificadoSenha { get; private set; }
    public DateTime? CertificadoValidade { get; private set; }
    public string? CertificadoCnpj { get; private set; }

    // CSC — Código de Segurança do Contribuinte (usado para gerar hash do QR Code da NFC-e)
    public string? CscId { get; private set; }      // ID do token (1-6 dígitos)
    public string? CscToken { get; private set; }   // Token CSC fornecido pela SEFAZ

    private readonly List<NotaFiscal> _notas = new();
    public IReadOnlyCollection<NotaFiscal> Notas => _notas.AsReadOnly();

    protected Empresa() { }

    public static Empresa Criar(
        Guid escritorioId,
        string razaoSocial, string nomeFantasia, string cnpj, string inscricaoEstadual,
        string logradouro, string numero, string bairro, string cidade, string uf,
        string cep, string codigoMunicipio, string telefone, string email,
        RegimeTributario regime, AmbienteSefaz ambiente,
        string? cnae = null)
    {
        return new Empresa
        {
            EscritorioId = escritorioId,
            RazaoSocial = razaoSocial,
            NomeFantasia = nomeFantasia,
            Cnpj = cnpj,
            InscricaoEstadual = inscricaoEstadual,
            Logradouro = logradouro,
            Numero = numero,
            Bairro = bairro,
            Cidade = cidade,
            Uf = uf,
            Cep = cep,
            CodigoMunicipio = codigoMunicipio,
            Telefone = telefone,
            Email = email,
            Cnae = cnae,
            RegimeTributario = regime,
            AmbienteSefaz = ambiente
        };
    }

    public void AtualizarCnae(string? cnae)
    {
        Cnae = cnae;
        SetUpdated();
    }

    public void AtualizarCsc(string? cscId, string? cscToken)
    {
        CscId = cscId;
        CscToken = cscToken;
        SetUpdated();
    }

    public void Atualizar(
        string razaoSocial, string nomeFantasia, string inscricaoEstadual,
        string logradouro, string numero, string bairro, string cidade, string uf,
        string cep, string codigoMunicipio, string telefone, string email,
        RegimeTributario regime, AmbienteSefaz ambiente,
        string? cnae, string? cscId, string? cscToken)
    {
        // CNPJ é imutável — chave fiscal não muda.
        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        InscricaoEstadual = inscricaoEstadual;
        Logradouro = logradouro;
        Numero = numero;
        Bairro = bairro;
        Cidade = cidade;
        Uf = uf;
        Cep = cep;
        CodigoMunicipio = codigoMunicipio;
        Telefone = telefone;
        Email = email;
        RegimeTributario = regime;
        AmbienteSefaz = ambiente;
        Cnae = cnae;
        CscId = cscId;
        CscToken = cscToken;
        SetUpdated();
    }

    public void AtualizarCertificado(byte[] bytes, string senha, DateTime validade, string cnpj)
    {
        CertificadoBytes = bytes;
        CertificadoSenha = senha;
        CertificadoValidade = validade;
        CertificadoCnpj = cnpj;
        SetUpdated();
    }

    public int ProximoNumeroNFe()
    {
        UltimoNumeronFe++;
        SetUpdated();
        return UltimoNumeronFe;
    }

    public int ProximoNumeroNFCe()
    {
        UltimoNumeronFCe++;
        SetUpdated();
        return UltimoNumeronFCe;
    }

    public bool CertificadoValido() =>
        CertificadoBytes != null && CertificadoValidade.HasValue && CertificadoValidade.Value > DateTime.UtcNow;
}
