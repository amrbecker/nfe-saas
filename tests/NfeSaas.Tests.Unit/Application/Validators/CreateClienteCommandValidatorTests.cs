using FluentAssertions;
using NfeSaas.Application.Commands.ClienteCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Validators;

namespace NfeSaas.Tests.Unit.Application.Validators;

public class CreateClienteCommandValidatorTests
{
    private readonly CreateClienteCommandValidator _validator = new();

    private static CreateClienteDto DtoValido(
        int tipoPessoa = 2, string? cpfCnpj = "12345678000195", string? email = "cli@x.com",
        string uf = "SP", string? ie = "111111111111", int indIe = 1) => new(
        tipoPessoa, cpfCnpj, "Cliente Teste", null, email, null,
        "Rua A", "1", null, "Centro", "Cidade", uf, "01310100", "3550308",
        ie, indIe);

    [Fact]
    public void ComDadosValidos_NaoRetornaErros()
    {
        var result = _validator.Validate(new CreateClienteCommand(Guid.NewGuid(), DtoValido()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ComEmailEmFormatoInvalido_RetornaErro()
    {
        var result = _validator.Validate(new CreateClienteCommand(Guid.NewGuid(), DtoValido(email: "nao-e-email")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Email"));
    }

    [Fact]
    public void ComCpfCnpjInvalido_RetornaErro()
    {
        var result = _validator.Validate(new CreateClienteCommand(Guid.NewGuid(), DtoValido(cpfCnpj: "11111111111111")));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("CpfCnpj"));
    }

    [Fact]
    public void ComPessoaFisicaSemCpf_RetornaErro()
    {
        var dto = DtoValido(tipoPessoa: 1, cpfCnpj: null, indIe: 9, ie: null);
        var result = _validator.Validate(new CreateClienteCommand(Guid.NewGuid(), dto));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("CpfCnpj"));
    }

    [Fact]
    public void ComEstrangeiroSemCpfCnpj_NaoRetornaErroDeDocumento()
    {
        var dto = DtoValido(tipoPessoa: 3, cpfCnpj: null, indIe: 9, ie: null);
        var result = _validator.Validate(new CreateClienteCommand(Guid.NewGuid(), dto));

        result.Errors.Should().NotContain(e => e.PropertyName.Contains("CpfCnpj"));
    }
}
