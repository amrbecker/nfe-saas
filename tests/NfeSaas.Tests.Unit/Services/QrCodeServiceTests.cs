using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Tests.Unit.Services;

public class QrCodeServiceTests
{
    private const string ChaveValida = "35260512345678000195550010000000011000000019";

    private static Empresa CriarEmpresa(
        string uf = "SP",
        AmbienteSefaz amb = AmbienteSefaz.Homologacao,
        string? cscId = null, string? cscToken = null)
    {
        var e = Empresa.Criar(Guid.NewGuid(), "X LTDA", "X", "12345678000195",
            "111111111111", "Rua A", "1", "Centro", "SP-Capital", uf,
            "01310100", "3550308", "11999999999", "x@x.com",
            RegimeTributario.RegimeNormal, amb);
        if (cscId != null && cscToken != null) e.AtualizarCsc(cscId, cscToken);
        return e;
    }

    private static NotaFiscal CriarNotaAutorizada(string chave = ChaveValida)
    {
        var n = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFCe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.MarcarEnviada("<x/>");
        n.Autorizar(chave, "PROT", "<r/>");
        return n;
    }

    [Fact]
    public void MontarUrlConsultaNFCe_SemChave_RetornaVazio()
    {
        var n = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFCe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        var e = CriarEmpresa();
        QrCodeService.MontarUrlConsultaNFCe(n, e).Should().BeEmpty();
    }

    [Fact]
    public void MontarUrlConsultaNFCe_SemCscConfigurado_RetornaUrlSemHash()
    {
        var n = CriarNotaAutorizada();
        var e = CriarEmpresa("SP", AmbienteSefaz.Homologacao);
        var url = QrCodeService.MontarUrlConsultaNFCe(n, e);

        url.Should().StartWith("https://www.homologacao.nfce.fazenda.sp.gov.br/qrcode");
        url.Should().Contain($"p={ChaveValida}|2|2");  // versao=2, tpAmb=2 (homolog)
        url.Should().NotContain("|abcd"); // nenhum hash
    }

    [Fact]
    public void MontarUrlConsultaNFCe_ComCscConfigurado_IncluiHashSha1()
    {
        var n = CriarNotaAutorizada();
        var e = CriarEmpresa("SP", AmbienteSefaz.Homologacao,
            cscId: "000001", cscToken: "TOKEN-CSC-EXEMPLO");
        var url = QrCodeService.MontarUrlConsultaNFCe(n, e);

        url.Should().Contain("|000001|");  // CscId no penúltimo segmento

        // Hash SHA1 (lowercase hex, 40 chars) deve aparecer no final
        var partes = url.Split('|');
        partes.Length.Should().Be(5);
        var hash = partes[4];
        hash.Should().HaveLength(40);
        hash.Should().MatchRegex("^[0-9a-f]{40}$");
    }

    [Theory]
    [InlineData("SP", AmbienteSefaz.Producao,    "https://www.nfce.fazenda.sp.gov.br/qrcode")]
    [InlineData("SP", AmbienteSefaz.Homologacao, "https://www.homologacao.nfce.fazenda.sp.gov.br/qrcode")]
    [InlineData("RJ", AmbienteSefaz.Producao,    "http://www4.fazenda.rj.gov.br/consultaNFCe/QRCode")]
    [InlineData("MG", AmbienteSefaz.Homologacao, "https://hnfce.fazenda.mg.gov.br/portalnfce/sistema/qrcode.xhtml")]
    [InlineData("PR", AmbienteSefaz.Producao,    "http://www.fazenda.pr.gov.br/nfce/qrcode")]
    public void MontarUrlConsultaNFCe_DeveUsarUrlPorUfEAmbiente(string uf, AmbienteSefaz amb, string urlEsperada)
    {
        var n = CriarNotaAutorizada();
        var e = CriarEmpresa(uf, amb);
        var url = QrCodeService.MontarUrlConsultaNFCe(n, e);
        url.Should().StartWith(urlEsperada);
    }

    [Fact]
    public void MontarUrlConsultaNFCe_UfDesconhecida_UsaSvrsAsFallback()
    {
        var n = CriarNotaAutorizada();
        var e = CriarEmpresa("AC", AmbienteSefaz.Homologacao);
        var url = QrCodeService.MontarUrlConsultaNFCe(n, e);
        url.Should().StartWith("https://www.svrs.rs.gov.br/nfce/consulta");
    }

    [Fact]
    public void MontarUrlConsultaNFe_DeveApontarParaPortalNacional()
    {
        var n = CriarNotaAutorizada();
        var url = QrCodeService.MontarUrlConsultaNFe(n);
        url.Should().Contain("nfe.fazenda.gov.br");
        url.Should().Contain($"chNFe={ChaveValida}");
    }

    [Fact]
    public void MontarUrlConsultaNFe_SemChave_RetornaVazio()
    {
        var n = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        QrCodeService.MontarUrlConsultaNFe(n).Should().BeEmpty();
    }

    [Fact]
    public void GerarQrCodePng_DeveRetornarBytesValidosPng()
    {
        var bytes = QrCodeService.GerarQrCodePng("https://example.com/test", pixelsPerModule: 4);
        bytes.Should().NotBeNullOrEmpty();
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be(0x50);
        bytes[2].Should().Be(0x4E);
        bytes[3].Should().Be(0x47);
    }

    [Fact]
    public void GerarCode128Png_ChaveAcesso_DeveRetornarPngValido()
    {
        var bytes = QrCodeService.GerarCode128Png(ChaveValida);
        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be(0x50);
    }

    [Fact]
    public void GerarCode128Png_StringVazia_RetornaArrayVazio()
    {
        QrCodeService.GerarCode128Png("").Should().BeEmpty();
        QrCodeService.GerarCode128Png(null!).Should().BeEmpty();
    }
}
