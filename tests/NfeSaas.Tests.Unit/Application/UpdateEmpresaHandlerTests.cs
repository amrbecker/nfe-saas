using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.EmpresaCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class UpdateEmpresaHandlerTests
{
    private readonly Mock<IEmpresaRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateEmpresaCommandHandler Handler() => new(_repo.Object, _uow.Object);

    private static Empresa Existente()
    {
        var e = Empresa.Criar(Guid.NewGuid(), "Antiga LTDA", "Antiga", "12345678000195",
            "111111111111", "Rua A", "1", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11999999999", "x@x.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);
        e.AtualizarCsc("000001", "TOKEN-EXISTENTE");
        return e;
    }

    private static UpdateEmpresaDto Dto(
        string razao = "Nova Razão",
        string ie = "111111111111",
        string uf = "SP",
        int regime = 3,
        string? cnae = null,
        string? cscId = null,
        string? cscToken = null) =>
        new(razao, "Fantasia", ie,
            "Logradouro", "1", "Bairro", "Cidade", uf, "01310100", "3550308",
            "11999999999", "novo@x.com",
            regime, 2, cnae, cscId, cscToken);

    [Fact]
    public async Task Update_DadosValidos_AtualizaERetornaSucesso()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto()), CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        empresa.RazaoSocial.Should().Be("Nova Razão");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_EmpresaNaoExiste_RetornaErro()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Empresa?)null);
        var result = await Handler().Handle(
            new UpdateEmpresaCommand(Guid.NewGuid(), Dto()), CancellationToken.None);
        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Update_RazaoSocialVazia_RetornaErro()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(razao: "")), CancellationToken.None);
        result.Erro.Should().Contain("Razão social");
    }

    [Fact]
    public async Task Update_UfInvalida_RetornaErro()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(uf: "ZZ")), CancellationToken.None);
        result.Erro.Should().Contain("UF");
    }

    [Fact]
    public async Task Update_IeInvalidaParaUf_RetornaErro()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(uf: "SP", ie: "1234")), CancellationToken.None);
        result.Erro.Should().Contain("Inscrição estadual");
    }

    [Fact]
    public async Task Update_RegimeTributarioInvalido_RetornaErro()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(regime: 99)), CancellationToken.None);
        result.Erro.Should().Contain("Regime");
    }

    [Fact]
    public async Task Update_CnaeInvalido_RetornaErro()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(cnae: "12345")), CancellationToken.None);
        result.Erro.Should().Contain("CNAE");
    }

    [Fact]
    public async Task Update_TokenCscVazio_PreservaTokenAtual()
    {
        var empresa = Existente();  // tem CscToken="TOKEN-EXISTENTE"
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        // DTO sem cscToken (vazio) mas com cscId existente
        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(cscId: "000001", cscToken: null)),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        empresa.CscToken.Should().Be("TOKEN-EXISTENTE");  // preservado
    }

    [Fact]
    public async Task Update_TokenCscPreenchido_SobrescreveAntigo()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(cscId: "000002", cscToken: "NOVO-TOKEN")),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        empresa.CscId.Should().Be("000002");
        empresa.CscToken.Should().Be("NOVO-TOKEN");
    }

    [Fact]
    public async Task Update_CscIdVazio_LimpaToken()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new UpdateEmpresaCommand(empresa.Id, Dto(cscId: null, cscToken: null)),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        empresa.CscId.Should().BeNull();
        empresa.CscToken.Should().BeNull();
    }
}
