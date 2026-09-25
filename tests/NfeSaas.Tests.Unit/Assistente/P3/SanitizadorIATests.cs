using FluentAssertions;
using NfeSaas.Application.Assistente.Servicos;

namespace NfeSaas.Tests.Unit.Assistente.P3;

public class SanitizadorIATests
{
    private readonly SanitizadorIA _s = new();

    // CPF/CNPJ fictícios com dígito verificador válido.
    private const string Cpf = "529.982.247-25";
    private const string CpfSemMascara = "52998224725";
    private const string Cnpj = "11.222.333/0001-81";

    [Theory]
    [InlineData("O cliente de CPF 529.982.247-25 pode receber nota?", "[CPF_1]")]
    [InlineData("CPF 52998224725 do consumidor", "[CPF_1]")]
    [InlineData("CNPJ 11.222.333/0001-81 está ativo?", "[CNPJ_1]")]
    [InlineData("CNPJ 11222333000181", "[CNPJ_1]")]
    [InlineData("mande para joao.silva@exemplo.com.br", "[EMAIL_1]")]
    [InlineData("liga no (11) 98765-4321", "[TELEFONE_1]")]
    [InlineData("celular 11987654321", "[TELEFONE_1]")]
    [InlineData("CEP 01310-100", "[CEP_1]")]
    [InlineData("cep: 01310100", "[CEP_1]")]
    [InlineData("chave 3526 0911 2223 3300 0181 5500 1000 0000 0110 0000 0019", "[CHAVE_ACESSO_1]")]
    public void Substitui_dados_pessoais_por_marcadores(string texto, string marcador)
    {
        var r = _s.Sanitizar(texto);
        r.Texto.Should().Contain(marcador);
        r.Quantidade.Should().BeGreaterThan(0);
        r.Texto.Should().NotMatchRegex(@"\d{8,}");
    }

    [Theory]
    [InlineData("O NCM 69120000 exige CEST?")]
    [InlineData("NCM 6912.00.00 e CFOP 5102 com CSOSN 102")]
    [InlineData("Rejeição 778: NCM inexistente")]
    [InlineData("Total de R$ 1.234,56 na nota 1482 série 1")]
    [InlineData("Emitida em 31/12/2026 com alíquota de 18%")]
    [InlineData("cClassTrib 000001 e CST 000")]
    public void Nao_mexe_em_codigos_fiscais_valores_e_datas(string texto)
    {
        var r = _s.Sanitizar(texto);
        r.Texto.Should().Be(texto);
        r.Quantidade.Should().Be(0);
    }

    [Fact]
    public void Mesmo_valor_recebe_o_mesmo_marcador_e_reidrata()
    {
        var mapa = new Dictionary<string, string>();
        var a = _s.Sanitizar($"CPF {Cpf} e de novo {CpfSemMascara}", mapa);
        var b = _s.Sanitizar($"outro trecho com {Cpf} e CNPJ {Cnpj}", mapa);

        a.Texto.Should().Be("CPF [CPF_1] e de novo [CPF_1]");
        b.Texto.Should().Be("outro trecho com [CPF_1] e CNPJ [CNPJ_1]");
        _s.Reidratar("Confira o [CPF_1] e o [CNPJ_1].", b.Substituicoes)
            .Should().Be($"Confira o {Cpf} e o {Cnpj}.");
    }

    [Fact]
    public void Reidratar_nao_confunde_marcador_1_com_10()
    {
        var mapa = new Dictionary<string, string> { ["[CPF_1]"] = "um", ["[CPF_10]"] = "dez" };
        _s.Reidratar("[CPF_10] [CPF_1]", mapa).Should().Be("dez um");
    }

    [Fact]
    public void Descreve_pessoa_sem_expor_documento()
    {
        var pf = _s.DescreverPessoa(Cpf, "sp", null);
        pf.Tipo.Should().Be("PF");
        pf.DocValido.Should().BeTrue();
        pf.Uf.Should().Be("SP");
        pf.IePresente.Should().BeFalse();

        var pj = _s.DescreverPessoa("11.222.333/0001-00", "MG", "ISENTO");
        pj.Tipo.Should().Be("PJ");
        pj.DocValido.Should().BeFalse();
        pj.IePresente.Should().BeFalse();
    }

    [Fact]
    public void Texto_vazio_retorna_vazio()
    {
        _s.Sanitizar(null).Texto.Should().BeEmpty();
        _s.Sanitizar("").Quantidade.Should().Be(0);
    }
}
