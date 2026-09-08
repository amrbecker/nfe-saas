using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.ConfiguracaoEmpresaCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class ConfiguracaoEmpresaHandlersTests
{
    private readonly Mock<IConfiguracaoEmpresaRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private SalvarConfiguracaoEmpresaCommandHandler SalvarHandler() => new(_repo.Object, _uow.Object);
    private GetConfiguracaoEmpresaQueryHandler GetHandler() => new(_repo.Object);

    private static ConfiguracaoEmpresaDto Dto(DateTime? concluidoEm = null) => new(
        (int)PerfilCliente.EmpresasMistasComSt,
        (int)TipoProduto.ProdutosComplexos,
        (int)VolumeNotas.De101A1000,
        (int)NivelAutomacao.SemiAutomatico,
        EmiteParaConsumidorFinal: true,
        OperaIcmsSt: true,
        (int)NivelRelatorio.Avancado,
        concluidoEm);

    [Fact]
    public async Task Salvar_QuandoNaoExisteConfiguracao_DeveCriarViaAddAsync()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetByEmpresaAsync(empresaId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((ConfiguracaoEmpresa?)null);

        var result = await SalvarHandler().Handle(
            new SalvarConfiguracaoEmpresaCommand(empresaId, Dto()), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PerfilCliente.Should().Be((int)PerfilCliente.EmpresasMistasComSt);
        result.TipoProduto.Should().Be((int)TipoProduto.ProdutosComplexos);
        result.VolumeNotas.Should().Be((int)VolumeNotas.De101A1000);
        result.NivelAutomacao.Should().Be((int)NivelAutomacao.SemiAutomatico);
        result.EmiteParaConsumidorFinal.Should().BeTrue();
        result.OperaIcmsSt.Should().BeTrue();
        result.NivelRelatorio.Should().Be((int)NivelRelatorio.Avancado);
        result.ConcluidoEm.Should().NotBeNull();

        _repo.Verify(r => r.AddAsync(It.IsAny<ConfiguracaoEmpresa>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<ConfiguracaoEmpresa>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Salvar_QuandoJaExisteConfiguracao_DeveAtualizarViaUpdateAsync()
    {
        var empresaId = Guid.NewGuid();
        var existente = ConfiguracaoEmpresa.Criar(
            empresaId,
            PerfilCliente.PequenasEmpresasSimples,
            TipoProduto.ServicosBasicos,
            VolumeNotas.Ate100,
            NivelAutomacao.Manual,
            consumidorFinal: false,
            operaSt: false,
            relatorio: NivelRelatorio.Basico);

        _repo.Setup(r => r.GetByEmpresaAsync(empresaId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        var novoDto = Dto();
        var result = await SalvarHandler().Handle(
            new SalvarConfiguracaoEmpresaCommand(empresaId, novoDto), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PerfilCliente.Should().Be((int)PerfilCliente.EmpresasMistasComSt);
        result.TipoProduto.Should().Be((int)TipoProduto.ProdutosComplexos);
        result.VolumeNotas.Should().Be((int)VolumeNotas.De101A1000);
        result.NivelAutomacao.Should().Be((int)NivelAutomacao.SemiAutomatico);
        result.EmiteParaConsumidorFinal.Should().BeTrue();
        result.OperaIcmsSt.Should().BeTrue();
        result.NivelRelatorio.Should().Be((int)NivelRelatorio.Avancado);

        _repo.Verify(r => r.UpdateAsync(existente, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<ConfiguracaoEmpresa>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_QuandoNaoExisteConfiguracao_RetornaNull()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetByEmpresaAsync(empresaId, It.IsAny<CancellationToken>()))
             .ReturnsAsync((ConfiguracaoEmpresa?)null);

        var result = await GetHandler().Handle(new GetConfiguracaoEmpresaQuery(empresaId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Get_QuandoExisteConfiguracao_RetornaDtoMapeado()
    {
        var empresaId = Guid.NewGuid();
        var existente = ConfiguracaoEmpresa.Criar(
            empresaId,
            PerfilCliente.ClientesExigentesComplexos,
            TipoProduto.ProdutosSimples,
            VolumeNotas.MaisDe1000,
            NivelAutomacao.Automatico,
            consumidorFinal: true,
            operaSt: false,
            relatorio: NivelRelatorio.Intermediario);

        _repo.Setup(r => r.GetByEmpresaAsync(empresaId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        var result = await GetHandler().Handle(new GetConfiguracaoEmpresaQuery(empresaId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PerfilCliente.Should().Be((int)PerfilCliente.ClientesExigentesComplexos);
        result.TipoProduto.Should().Be((int)TipoProduto.ProdutosSimples);
        result.VolumeNotas.Should().Be((int)VolumeNotas.MaisDe1000);
        result.NivelAutomacao.Should().Be((int)NivelAutomacao.Automatico);
        result.EmiteParaConsumidorFinal.Should().BeTrue();
        result.OperaIcmsSt.Should().BeFalse();
        result.NivelRelatorio.Should().Be((int)NivelRelatorio.Intermediario);
        result.ConcluidoEm.Should().Be(existente.ConcluidoEm);
    }
}
