using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class EmpresaTests
{
    private static Empresa CriarEmpresa() =>
        Empresa.Criar(Guid.NewGuid(), "Empresa Teste LTDA", "Empresa Teste", "12345678000195",
            "IE123", "Rua A", "100", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11999999999", "teste@empresa.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);

    [Fact]
    public void ProximoNumeroNFe_DeveIncrementarSequencialmente()
    {
        var empresa = CriarEmpresa();

        var n1 = empresa.ProximoNumeroNFe();
        var n2 = empresa.ProximoNumeroNFe();
        var n3 = empresa.ProximoNumeroNFe();

        n1.Should().Be(1);
        n2.Should().Be(2);
        n3.Should().Be(3);
        empresa.UltimoNumeronFe.Should().Be(3);
    }

    [Fact]
    public void ProximoNumeroNFCe_DeveIncrementarSeparadoDeNFe()
    {
        var empresa = CriarEmpresa();

        empresa.ProximoNumeroNFe(); // NFe = 1
        var nfce1 = empresa.ProximoNumeroNFCe();
        var nfce2 = empresa.ProximoNumeroNFCe();

        nfce1.Should().Be(1);
        nfce2.Should().Be(2);
        empresa.UltimoNumeronFe.Should().Be(1);  // NFe não avançou
        empresa.UltimoNumeronFCe.Should().Be(2);
    }

    [Fact]
    public void CertificadoValido_QuandoNaoConfigurado_RetornaFalso()
    {
        var empresa = CriarEmpresa();
        empresa.CertificadoValido().Should().BeFalse();
    }

    [Fact]
    public void CertificadoValido_QuandoExpirado_RetornaFalso()
    {
        var empresa = CriarEmpresa();
        empresa.AtualizarCertificado(
            new byte[] { 1, 2, 3 }, "senha",
            DateTime.UtcNow.AddDays(-1),  // expirado ontem
            "12345678000195");

        empresa.CertificadoValido().Should().BeFalse();
    }

    [Fact]
    public void CertificadoValido_QuandoVigente_RetornaVerdadeiro()
    {
        var empresa = CriarEmpresa();
        empresa.AtualizarCertificado(
            new byte[] { 1, 2, 3 }, "senha",
            DateTime.UtcNow.AddYears(1),  // válido por 1 ano
            "12345678000195");

        empresa.CertificadoValido().Should().BeTrue();
    }

    [Fact]
    public void AtualizarCertificado_DevePersistirTodosOsDados()
    {
        var empresa = CriarEmpresa();
        var bytes = new byte[] { 0x30, 0x82, 0x01 };
        var validade = DateTime.UtcNow.AddYears(2);

        empresa.AtualizarCertificado(bytes, "MinhaS3nh@", validade, "12345678000195");

        empresa.CertificadoBytes.Should().BeEquivalentTo(bytes);
        empresa.CertificadoSenha.Should().Be("MinhaS3nh@");
        empresa.CertificadoValidade.Should().BeCloseTo(validade, TimeSpan.FromSeconds(1));
        empresa.CertificadoCnpj.Should().Be("12345678000195");
    }

    [Fact]
    public void Criar_DevePertencerAoEscritorio()
    {
        var escritorioId = Guid.NewGuid();
        var empresa = Empresa.Criar(escritorioId, "Empresa X", "X", "98765432000111",
            "IE", "Rua", "1", "Bairro", "Cidade", "SP",
            "12345678", "1234567", "11000000000", "x@x.com",
            RegimeTributario.RegimeNormal, AmbienteSefaz.Producao);

        empresa.EscritorioId.Should().Be(escritorioId);
    }
}
