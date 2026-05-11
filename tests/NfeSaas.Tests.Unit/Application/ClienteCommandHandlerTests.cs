using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.ClienteCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class ClienteCommandHandlerTests
{
    private readonly Mock<IClienteRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CreateClienteCommandHandler Handler() => new(_repo.Object, _uow.Object);

    private static CreateClienteDto Dto(
        int tipoPessoa = 2, string? cpfCnpj = "12345678000195",
        string uf = "SP", string ie = "111111111111", int indIe = 1) =>
        new(tipoPessoa, cpfCnpj, "Cliente", null, "cli@x.com", null,
            "Rua A", "1", null, "Centro", "Cidade", uf, "01310100", "3550308",
            ie, indIe);

    [Fact]
    public async Task Create_DadosValidos_RetornaCliente()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetByCpfCnpjAsync(empresaId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Cliente?)null);

        var result = await Handler().Handle(
            new CreateClienteCommand(empresaId, Dto()), CancellationToken.None);

        result.Erro.Should().BeNull();
        result.Cliente.Should().NotBeNull();
        result.Cliente!.RazaoSocial.Should().Be("Cliente");
        _repo.Verify(r => r.AddAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_PfSemCpf_RetornaErro()
    {
        var dto = Dto(tipoPessoa: 1, cpfCnpj: null, indIe: 9, ie: "");
        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().Contain("CPF/CNPJ");
    }

    [Fact]
    public async Task Create_CnpjInvalido_RetornaErro()
    {
        var dto = Dto(cpfCnpj: "11111111111111");  // mesmo dígito repetido = inválido
        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().Contain("CNPJ");
    }

    [Fact]
    public async Task Create_UfInvalida_RetornaErro()
    {
        var dto = Dto(uf: "XX");
        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().Contain("UF");
    }

    [Fact]
    public async Task Create_IndicadorContribuinteSemIe_RetornaErro()
    {
        // indIe=1 (Contribuinte) exige IE válida; passando vazia
        var dto = Dto(indIe: 1, ie: "");
        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().Contain("Inscrição estadual");
    }

    [Fact]
    public async Task Create_IndicadorContribuinteIeInvalidaParaUf_RetornaErro()
    {
        // SP exige IE com 12 dígitos. Passando 8 = inválida
        var dto = Dto(uf: "SP", ie: "12345678");
        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().Contain("Inscrição estadual inválida");
    }

    [Fact]
    public async Task Create_NaoContribuinte_NaoExigeIe()
    {
        var dto = Dto(indIe: 9, ie: "");
        _repo.Setup(r => r.GetByCpfCnpjAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Cliente?)null);

        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().BeNull();
        result.Cliente.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Estrangeiro_AceitaSemCpfCnpj()
    {
        var dto = Dto(tipoPessoa: 3, cpfCnpj: null, indIe: 9, ie: "");
        _repo.Setup(r => r.GetByCpfCnpjAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Cliente?)null);

        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().BeNull();
    }

    [Fact]
    public async Task Create_CpfCnpjJaExistente_RetornaConflito()
    {
        var empresaId = Guid.NewGuid();
        var existente = Cliente.Criar(empresaId, TipoPessoa.PessoaJuridica, "12345678000195",
            "X", null, null, null, "R", "1", null, "B", "C", "SP", "01310100", "3550308",
            null, IndicadorIeDestinatario.NaoContribuinte);
        _repo.Setup(r => r.GetByCpfCnpjAsync(empresaId, "12345678000195", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        var dto = Dto(indIe: 9, ie: "");
        var result = await Handler().Handle(
            new CreateClienteCommand(empresaId, dto), CancellationToken.None);

        result.Erro.Should().Contain("Já existe cliente");
    }

    [Fact]
    public async Task Create_CepInvalido_RetornaErro()
    {
        // CEP com 7 dígitos (inválido)
        var dto = new CreateClienteDto(2, "12345678000195",
            "X", null, null, null, "R", "1", null, "B", "C", "SP",
            "1234567", "3550308", null, 9);
        var result = await Handler().Handle(
            new CreateClienteCommand(Guid.NewGuid(), dto), CancellationToken.None);

        result.Erro.Should().Contain("CEP");
    }
}
