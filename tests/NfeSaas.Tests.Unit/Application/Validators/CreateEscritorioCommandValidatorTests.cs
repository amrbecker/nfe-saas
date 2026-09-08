using FluentAssertions;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Validators;

namespace NfeSaas.Tests.Unit.Application.Validators;

public class CreateEscritorioCommandValidatorTests
{
    private readonly CreateEscritorioCommandValidator _validator = new();

    private static CreateEscritorioDto DtoValido(
        string cnpj = "11222333000181", string email = "contato@escritorio.com.br",
        int plano = 1, string senhaAdmin = "SenhaForte123") => new(
        "Escritório Teste LTDA", "Escritório Teste", cnpj, email, "11999999999",
        plano, "Admin", "admin@escritorio.com.br", senhaAdmin);

    [Fact]
    public void ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(new CreateEscritorioCommand(DtoValido()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ComCnpjInvalido_RetornaErro()
    {
        var result = _validator.Validate(new CreateEscritorioCommand(DtoValido(cnpj: "11111111111111")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Cnpj"));
    }

    [Fact]
    public void ComEmailInvalido_RetornaErro()
    {
        var result = _validator.Validate(new CreateEscritorioCommand(DtoValido(email: "nao-e-email")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith("Email"));
    }

    [Fact]
    public void ComPlanoInexistente_RetornaErro()
    {
        var result = _validator.Validate(new CreateEscritorioCommand(DtoValido(plano: 99)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Plano"));
    }

    [Fact]
    public void ComSenhaCurta_RetornaErro()
    {
        var result = _validator.Validate(new CreateEscritorioCommand(DtoValido(senhaAdmin: "123")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("SenhaAdmin"));
    }

    [Fact]
    public void ComRazaoSocialVazia_RetornaErro()
    {
        var dto = DtoValido() with { RazaoSocial = "" };
        var result = _validator.Validate(new CreateEscritorioCommand(dto));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RazaoSocial"));
    }
}
