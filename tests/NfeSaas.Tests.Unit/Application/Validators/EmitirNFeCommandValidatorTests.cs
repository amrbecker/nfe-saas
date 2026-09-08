using FluentAssertions;
using NfeSaas.Application.Commands.EmitirNFe;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Validators;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Application.Validators;

public class EmitirNFeCommandValidatorTests
{
    private readonly EmitirNFeCommandValidator _validator = new();

    private static ImpostosItemDto ImpostosValidos() => new(
        OrigemMercadoria.Nacional, CstIcms.Tributada, 18, null, false, null, null,
        CstPisCofins.TributadaAliquotaBasica, 1.65m, CstPisCofins.TributadaAliquotaBasica, 7.6m);

    private static ItemNotaDto ItemValido() => new(
        "PROD1", "Produto Teste", "12345678", null, "5102", "UN",
        2, 10m, 0m, null, ImpostosValidos());

    private static DestinatarioDto DestinatarioValido() => new(
        "12345678000195", "Dest", "d@x.com", TipoPessoa.PessoaJuridica,
        "Rua", "1", null, "Centro", "Cidade", "SP", "01310100", "3550308", null);

    private static EmitirNotaFiscalDto DtoValido(List<ItemNotaDto>? itens = null) => new(
        TipoNota.NFe, FinalidadeNota.Normal, TipoOperacao.Saida,
        DestinatarioValido(),
        itens ?? new List<ItemNotaDto> { ItemValido() },
        new TransporteDto(ModalidadeFrete.SemFrete, null, null, 0, 0),
        new PagamentoDto("01", 20m),
        null);

    private static EmitirNFeCommand Command(EmitirNotaFiscalDto? dados = null, Guid? empresaId = null, Guid? usuarioId = null) =>
        new(empresaId ?? Guid.NewGuid(), usuarioId ?? Guid.NewGuid(), dados ?? DtoValido());

    [Fact]
    public void ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(Command());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ComEmpresaIdVazio_RetornaErro()
    {
        var result = _validator.Validate(Command(empresaId: Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmpresaId");
    }

    [Fact]
    public void ComListaDeItensVazia_RetornaErro()
    {
        var result = _validator.Validate(Command(dados: DtoValido(itens: new List<ItemNotaDto>())));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("ao menos um item"));
    }

    [Fact]
    public void ComQuantidadeNegativaOuZero_RetornaErro()
    {
        var itemInvalido = ItemValido() with { Quantidade = 0 };
        var result = _validator.Validate(Command(dados: DtoValido(itens: new List<ItemNotaDto> { itemInvalido })));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Quantidade"));
    }

    [Fact]
    public void ComValorUnitarioNegativo_RetornaErro()
    {
        var itemInvalido = ItemValido() with { ValorUnitario = -5m };
        var result = _validator.Validate(Command(dados: DtoValido(itens: new List<ItemNotaDto> { itemInvalido })));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Valor unitário"));
    }

    [Fact]
    public void ComFreteOuSeguroNegativo_RetornaErro()
    {
        var dados = DtoValido() with { Transporte = new TransporteDto(ModalidadeFrete.SemFrete, null, null, -1m, 0) };
        var result = _validator.Validate(Command(dados: dados));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("frete"));
    }

    [Fact]
    public void ComValorDePagamentoNegativo_RetornaErro()
    {
        var dados = DtoValido() with { Pagamento = new PagamentoDto("01", -10m) };
        var result = _validator.Validate(Command(dados: dados));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("pagamento"));
    }
}
