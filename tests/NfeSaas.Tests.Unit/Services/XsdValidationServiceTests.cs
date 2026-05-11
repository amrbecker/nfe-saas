using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NfeSaas.Infrastructure.Services;

namespace NfeSaas.Tests.Unit.Services;

public class XsdValidationServiceTests
{
    private readonly XsdValidationService _service = new(NullLogger<XsdValidationService>.Instance);

    [Fact]
    public void Constructor_CarregaSchemasDePastaSchemas()
    {
        // Os XSDs skeletons são copiados para AppContext.BaseDirectory/Schemas no build
        _service.TemSchemasCarregados.Should().BeTrue();
        _service.TotalSchemasCarregados.Should().BeGreaterThan(0);
        _service.ErrosCarga.Should().BeEmpty();
    }

    [Fact]
    public void Validar_XmlMalFormado_RetornaErroDeFormacao()
    {
        var resultado = _service.Validar("<invalid>");
        resultado.Pulada.Should().BeFalse();
        resultado.Valido.Should().BeFalse();
        resultado.Erros.Should().NotBeEmpty();
    }

    [Fact]
    public void Validar_XmlVazio_RetornaErroVazio()
    {
        var resultado = _service.Validar("");
        resultado.Valido.Should().BeFalse();
        resultado.Erros.Should().Contain(e => e.Contains("vazio", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_NfeProcMinimo_DeveDetectarErrosEstruturais()
    {
        // nfeProc sem versao (atributo obrigatório) e sem NFe interno
        var xml = """
<?xml version="1.0" encoding="UTF-8"?>
<nfeProc xmlns="http://www.portalfiscal.inf.br/nfe">
</nfeProc>
""";
        var resultado = _service.Validar(xml);
        resultado.Valido.Should().BeFalse();
        resultado.Erros.Should().NotBeEmpty();
    }

    [Fact]
    public void Validar_InutilizacaoComCamposObrigatorios_QuandoBemFormada_PassaSchema()
    {
        // Estrutura mínima válida pelo skeleton
        var xml = """
<?xml version="1.0" encoding="UTF-8"?>
<inutNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
  <infInut Id="ID35261234567800019555001000000001000000001">
    <tpAmb>2</tpAmb>
    <xServ>INUTILIZAR</xServ>
    <cUF>35</cUF>
    <ano>26</ano>
    <CNPJ>12345678000195</CNPJ>
    <mod>55</mod>
    <serie>1</serie>
    <nNFIni>1</nNFIni>
    <nNFFin>1</nNFFin>
    <xJust>Quebra de sequência por descarte da nota.</xJust>
  </infInut>
</inutNFe>
""";
        var resultado = _service.Validar(xml);
        resultado.Valido.Should().BeTrue($"deveria validar: erros = {string.Join("; ", resultado.Erros)}");
    }

    [Fact]
    public void Validar_InutilizacaoSemCnpj_DeveFalhar()
    {
        var xml = """
<?xml version="1.0" encoding="UTF-8"?>
<inutNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
  <infInut Id="ID35261234567800019555001000000001000000001">
    <tpAmb>2</tpAmb>
    <xServ>INUTILIZAR</xServ>
    <cUF>35</cUF>
    <ano>26</ano>
    <mod>55</mod>
    <serie>1</serie>
    <nNFIni>1</nNFIni>
    <nNFFin>1</nNFFin>
    <xJust>Justificativa válida com mais de 15 chars.</xJust>
  </infInut>
</inutNFe>
""";
        var resultado = _service.Validar(xml);
        resultado.Valido.Should().BeFalse();
    }

    [Fact]
    public void Validar_InutilizacaoComJustificativaCurta_DeveFalhar()
    {
        var xml = """
<?xml version="1.0" encoding="UTF-8"?>
<inutNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
  <infInut Id="ID35261234567800019555001000000001000000001">
    <tpAmb>2</tpAmb>
    <xServ>INUTILIZAR</xServ>
    <cUF>35</cUF>
    <ano>26</ano>
    <CNPJ>12345678000195</CNPJ>
    <mod>55</mod>
    <serie>1</serie>
    <nNFIni>1</nNFIni>
    <nNFFin>1</nNFFin>
    <xJust>curto</xJust>
  </infInut>
</inutNFe>
""";
        var resultado = _service.Validar(xml);
        resultado.Valido.Should().BeFalse();
        resultado.Erros.Should().NotBeEmpty();
    }
}
