using FluentAssertions;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Validators;

namespace NfeSaas.Tests.Unit.Application.Validators;

public class CreateEmpresaCommandValidatorTests
{
    private readonly CreateEmpresaCommandValidator _validator = new();

    private static CreateEmpresaDto DtoValido(
        string cnpj = "11222333000181", string uf = "SP", string cep = "01310100",
        string codigoMunicipio = "3550308") => new(
        "Empresa Teste LTDA", "Empresa Teste", cnpj, "111111111111",
        "Rua A", "1", "Centro", "Cidade", uf, cep, codigoMunicipio,
        "11999999999", "empresa@teste.com.br", 3, 2, null);

    [Fact]
    public void ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(new CreateEmpresaCommand(Guid.NewGuid(), DtoValido()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ComCnpjInvalido_RetornaErro()
    {
        var result = _validator.Validate(new CreateEmpresaCommand(Guid.NewGuid(), DtoValido(cnpj: "00000000000000")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Cnpj"));
    }

    [Fact]
    public void ComUfComTresLetras_RetornaErro()
    {
        var result = _validator.Validate(new CreateEmpresaCommand(Guid.NewGuid(), DtoValido(uf: "SPX")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Uf"));
    }

    [Fact]
    public void ComCepSemOitoDigitos_RetornaErro()
    {
        var result = _validator.Validate(new CreateEmpresaCommand(Guid.NewGuid(), DtoValido(cep: "123")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Cep"));
    }

    [Fact]
    public void ComEscritorioIdVazio_RetornaErro()
    {
        var result = _validator.Validate(new CreateEmpresaCommand(Guid.Empty, DtoValido()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EscritorioId");
    }
}
