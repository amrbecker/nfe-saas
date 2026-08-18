using System.Xml;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Tests.Unit.Services;

public class XmlNFeServiceTests
{
    private readonly XmlNFeService _service;
    private readonly XsdValidationService _xsd;

    public XmlNFeServiceTests()
    {
        _xsd = new XsdValidationService(NullLogger<XsdValidationService>.Instance);
        _service = new XmlNFeService(_xsd);
    }

    private static Empresa CriarEmpresa(string uf = "SP", string cnpj = "12345678000195") =>
        Empresa.Criar(Guid.NewGuid(), "Empresa LTDA", "Empresa", cnpj,
            "111111111111", "Rua A", "100", "Centro", "São Paulo", uf,
            "01310100", "3550308", "11999999999", "test@test.com",
            RegimeTributario.RegimeNormal, AmbienteSefaz.Homologacao);

    private static NotaFiscal CriarNotaCompleta(Empresa empresa, string destCnpj = "98765432000111", string destUf = "SP")
    {
        var nota = NotaFiscal.Criar(empresa.Id, TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.SetDestinatario(destCnpj, "Destinatário LTDA", "dest@test.com",
            TipoPessoa.PessoaJuridica, "Av Dest", "200", "Bairro", "Cidade",
            destUf, "01310200", "3550308", "ISENTO");
        var item = ItemNotaFiscal.Criar(nota.Id, 1, "PROD01", "Produto Teste",
            "12345678", "5102", "UN", 2m, 100m);
        item.SetIcms(OrigemMercadoria.Nacional, CstIcms.Tributada, 200m, 18m);
        item.SetPis(CstPisCofins.TributadaAliquotaBasica, 200m, 1.65m);
        item.SetCofins(CstPisCofins.TributadaAliquotaBasica, 200m, 7.6m);
        nota.AdicionarItem(item);
        nota.SetTransporte(ModalidadeFrete.SemFrete);
        nota.SetPagamento("01", 200m);
        return nota;
    }

    [Fact]
    public void GerarXmlNFe_DeveProduzirXmlBemFormado()
    {
        var empresa = CriarEmpresa();
        var nota = CriarNotaCompleta(empresa);

        var xml = _service.GerarXmlNFe(nota, empresa);

        xml.Should().NotBeNullOrWhiteSpace();
        var doc = new XmlDocument();
        Action a = () => doc.LoadXml(xml);
        a.Should().NotThrow();
    }

    [Fact]
    public void GerarXmlNFe_DeveConterTagsObrigatorias()
    {
        var empresa = CriarEmpresa();
        var nota = CriarNotaCompleta(empresa);
        var xml = _service.GerarXmlNFe(nota, empresa);

        xml.Should().Contain("<nfeProc");
        xml.Should().Contain("<NFe");
        xml.Should().Contain("<infNFe");
        xml.Should().Contain("<ide>");
        xml.Should().Contain("<emit>");
        xml.Should().Contain("<dest>");
        xml.Should().Contain("<det");
        xml.Should().Contain("<total>");
        xml.Should().Contain("<transp>");
        xml.Should().Contain("<pag>");
    }

    [Fact]
    public void GerarXmlNFe_DeveIncluirCnpjDoEmitente()
    {
        var empresa = CriarEmpresa(cnpj: "12345678000195");
        var nota = CriarNotaCompleta(empresa);
        var xml = _service.GerarXmlNFe(nota, empresa);

        xml.Should().Contain("<CNPJ>12345678000195</CNPJ>");
    }

    [Fact]
    public void GerarXmlNFe_ItemComIpi_DeveRenderizarBlocoIpi()
    {
        var empresa = CriarEmpresa();
        var nota = CriarNotaCompleta(empresa);
        var item = nota.Itens.First();
        item.SetIpi("50", baseCalculo: 200m, aliquota: 10m);

        var xml = _service.GerarXmlNFe(nota, empresa);
        xml.Should().Contain("<IPI>");
        xml.Should().Contain("<IPITrib>");
        xml.Should().Contain("<vIPI>20.00</vIPI>");
    }

    [Fact]
    public void GerarXmlNFe_ItemComCsosn_DeveUsarBlocoIcmsSn()
    {
        var empresa = CriarEmpresa();
        var nota = NotaFiscal.Criar(empresa.Id, TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        nota.SetDestinatario("12345678901", "Cliente PF", null, TipoPessoa.PessoaFisica,
            "Rua", "1", "Bairro", "Cidade", "SP", "01000000", "3550308");
        var item = ItemNotaFiscal.Criar(nota.Id, 1, "P1", "Item", "12345678", "5102", "UN", 1m, 100m);
        item.SetIcmsSimples(OrigemMercadoria.Nacional, CsosnIcms.TributadaSemPermissaoCredito, 100m, 0m);
        item.SetPis(CstPisCofins.TributadaAliquotaBasica, 100m, 0.65m);
        item.SetCofins(CstPisCofins.TributadaAliquotaBasica, 100m, 3m);
        nota.AdicionarItem(item);
        nota.SetTransporte(ModalidadeFrete.SemFrete);
        nota.SetPagamento("01", 100m);

        var xml = _service.GerarXmlNFe(nota, empresa);
        xml.Should().Contain("<ICMSSN102>");
        xml.Should().Contain("<CSOSN>102</CSOSN>");
        xml.Should().NotContain("<CST>00</CST>");  // Simples não deve emitir CST
    }

    [Fact]
    public void GerarXmlNFe_ItemComDifal_DeveRenderizarBlocoIcmsUfDest()
    {
        var empresa = CriarEmpresa(uf: "SP");
        var nota = CriarNotaCompleta(empresa, destUf: "RJ");
        var item = nota.Itens.First();
        item.SetDifal(baseCalculo: 200m, aliquotaInternaUfDestino: 20m, aliquotaInterestadual: 12m);

        var xml = _service.GerarXmlNFe(nota, empresa);
        xml.Should().Contain("<ICMSUFDest>");
        xml.Should().Contain("<vICMSUFDest>");
    }

    [Fact]
    public void GerarXmlNFe_ItemComFcp_DeveRenderizarValores()
    {
        var empresa = CriarEmpresa();
        var nota = CriarNotaCompleta(empresa);
        var item = nota.Itens.First();
        item.SetFcp(baseCalculo: 200m, aliquota: 2m);

        var xml = _service.GerarXmlNFe(nota, empresa);
        xml.Should().Contain("<vFCP>4.00</vFCP>");
    }

    [Fact]
    public void GerarXmlCancelamento_DeveProduzirXmlValido()
    {
        var empresa = CriarEmpresa();
        var xml = _service.GerarXmlCancelamento(
            "35260512345678000195550010000000011000000019",
            "135260000000001",
            "Cancelamento por erro de digitação no destinatário.",
            empresa);

        xml.Should().Contain("<envEvento");
        xml.Should().Contain("<infEvento");
        xml.Should().Contain("<tpEvento>110111</tpEvento>");
        xml.Should().Contain("<nProt>135260000000001</nProt>");
        xml.Should().Contain("<xJust>Cancelamento por erro de digitação no destinatário.</xJust>");
    }

    [Fact]
    public void GerarXmlCce_DeveProduzirXmlValido()
    {
        var empresa = CriarEmpresa();
        var xml = _service.GerarXmlCce(
            "35260512345678000195550010000000011000000019",
            sequencial: 1,
            "Correção de CFOP: o correto é 5102.",
            empresa);

        xml.Should().Contain("<envEvento");
        xml.Should().Contain("<tpEvento>110110</tpEvento>");
        xml.Should().Contain("<nSeqEvento>1</nSeqEvento>");
        xml.Should().Contain("<xCorrecao>");
        xml.Should().Contain("<xCondUso>");
    }

    [Fact]
    public void GerarXmlInutilizacao_DeveProduzirXmlValido()
    {
        var empresa = CriarEmpresa();
        var xml = _service.GerarXmlInutilizacao(empresa, ano: 2026, TipoNota.NFe,
            serie: 1, numIni: 1, numFin: 5, "Quebra de sequência por descarte de notas.");

        xml.Should().Contain("<inutNFe");
        xml.Should().Contain("<xServ>INUTILIZAR</xServ>");
        xml.Should().Contain("<nNFIni>1</nNFIni>");
        xml.Should().Contain("<nNFFin>5</nNFFin>");
        xml.Should().Contain("<ano>26</ano>");
    }

    [Fact]
    public void GerarXmlManifestacao_Confirmacao_DeveTerTpEventoCorreto()
    {
        var empresa = CriarEmpresa();
        var xml = _service.GerarXmlManifestacao(
            "35260512345678000195550010000000011000000019",
            TipoEventoFiscal.ManifestacaoConfirmacao, "", empresa);

        xml.Should().Contain("<tpEvento>210200</tpEvento>");
        xml.Should().Contain("<descEvento>Confirmacao da Operacao</descEvento>");
    }

    [Fact]
    public void GerarXmlManifestacao_OperacaoNaoRealizada_IncluiJustificativa()
    {
        var empresa = CriarEmpresa();
        var xml = _service.GerarXmlManifestacao(
            "35260512345678000195550010000000011000000019",
            TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada,
            "Mercadoria não foi entregue.", empresa);

        xml.Should().Contain("<tpEvento>210240</tpEvento>");
        xml.Should().Contain("<xJust>Mercadoria não foi entregue.</xJust>");
    }

    [Fact]
    public void GerarXmlManifestacao_Confirmacao_NaoIncluiJustificativa()
    {
        var empresa = CriarEmpresa();
        var xml = _service.GerarXmlManifestacao(
            "35260512345678000195550010000000011000000019",
            TipoEventoFiscal.ManifestacaoConfirmacao, "qualquer texto", empresa);

        xml.Should().NotContain("<xJust>");
    }

    [Fact]
    public void ValidarXml_XmlMalFormado_RetornaErros()
    {
        var ok = _service.ValidarXml("<not closed", out var erros);
        ok.Should().BeFalse();
        erros.Should().NotBeEmpty();
    }
}
