using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.EmpresaCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class AjustarNumeracaoEmpresaHandlerTests
{
    private readonly Mock<IEmpresaRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AjustarNumeracaoEmpresaCommandHandler Handler() => new(_repo.Object, _uow.Object);

    private static Empresa Existente() =>
        Empresa.Criar(Guid.NewGuid(), "Empresa LTDA", "Empresa", "12345678000195",
            "111111111111", "Rua A", "1", "Centro", "São Paulo", "SP",
            "01310100", "3550308", "11999999999", "x@x.com",
            RegimeTributario.SimplesNacional, AmbienteSefaz.Homologacao);

    [Fact]
    public async Task Ajustar_ContinuaNumeracaoExterna_AtualizaAmbosOsTipos()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new AjustarNumeracaoEmpresaCommand(empresa.Id, new AjustarNumeracaoDto(44, 10)),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        empresa.UltimoNumeronFe.Should().Be(44);
        empresa.UltimoNumeronFCe.Should().Be(10);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ajustar_ApenasNFe_NaoAlteraNFCe()
    {
        var empresa = Existente();
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new AjustarNumeracaoEmpresaCommand(empresa.Id, new AjustarNumeracaoDto(44, null)),
            CancellationToken.None);

        result.Sucesso.Should().BeTrue();
        empresa.UltimoNumeronFe.Should().Be(44);
        empresa.UltimoNumeronFCe.Should().Be(0);
    }

    [Fact]
    public async Task Ajustar_EmpresaNaoExiste_RetornaErro()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Empresa?)null);

        var result = await Handler().Handle(
            new AjustarNumeracaoEmpresaCommand(Guid.NewGuid(), new AjustarNumeracaoDto(44, null)),
            CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Ajustar_RetrocedeAbaixoDoJaEmitido_RetornaErroENaoSalva()
    {
        var empresa = Existente();
        empresa.ProximoNumeroNFe();
        empresa.ProximoNumeroNFe(); // UltimoNumeronFe = 2
        _repo.Setup(r => r.GetByIdAsync(empresa.Id, It.IsAny<CancellationToken>())).ReturnsAsync(empresa);

        var result = await Handler().Handle(
            new AjustarNumeracaoEmpresaCommand(empresa.Id, new AjustarNumeracaoDto(1, null)),
            CancellationToken.None);

        result.Sucesso.Should().BeFalse();
        empresa.UltimoNumeronFe.Should().Be(2);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
