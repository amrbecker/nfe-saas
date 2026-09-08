using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class ConfiguracaoEmpresaTests
{
    private static Guid EmpresaId => Guid.NewGuid();

    [Fact]
    public void Criar_DeveSetarTodosOsCamposCorretamente()
    {
        var empresaId = EmpresaId;

        var config = ConfiguracaoEmpresa.Criar(
            empresaId,
            PerfilCliente.EmpresasMistasComSt,
            TipoProduto.ProdutosComplexos,
            VolumeNotas.De101A1000,
            NivelAutomacao.SemiAutomatico,
            consumidorFinal: true,
            operaSt: true,
            relatorio: NivelRelatorio.Avancado);

        config.EmpresaId.Should().Be(empresaId);
        config.PerfilCliente.Should().Be(PerfilCliente.EmpresasMistasComSt);
        config.TipoProduto.Should().Be(TipoProduto.ProdutosComplexos);
        config.VolumeNotas.Should().Be(VolumeNotas.De101A1000);
        config.NivelAutomacao.Should().Be(NivelAutomacao.SemiAutomatico);
        config.EmiteParaConsumidorFinal.Should().BeTrue();
        config.OperaIcmsSt.Should().BeTrue();
        config.NivelRelatorio.Should().Be(NivelRelatorio.Avancado);
    }

    [Fact]
    public void Criar_DeveSetarConcluidoEmProximoDeAgora()
    {
        var config = ConfiguracaoEmpresa.Criar(
            EmpresaId,
            PerfilCliente.PequenasEmpresasSimples,
            TipoProduto.ServicosBasicos,
            VolumeNotas.Ate100,
            NivelAutomacao.Manual,
            consumidorFinal: false,
            operaSt: false,
            relatorio: NivelRelatorio.Basico);

        config.ConcluidoEm.Should().NotBeNull();
        config.ConcluidoEm!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Atualizar_DeveAlterarCamposParaNovosValores()
    {
        var config = ConfiguracaoEmpresa.Criar(
            EmpresaId,
            PerfilCliente.PequenasEmpresasSimples,
            TipoProduto.ServicosBasicos,
            VolumeNotas.Ate100,
            NivelAutomacao.Manual,
            consumidorFinal: false,
            operaSt: false,
            relatorio: NivelRelatorio.Basico);

        config.Atualizar(
            PerfilCliente.ClientesExigentesComplexos,
            TipoProduto.ProdutosSimples,
            VolumeNotas.MaisDe1000,
            NivelAutomacao.Automatico,
            consumidorFinal: true,
            operaSt: true,
            relatorio: NivelRelatorio.Intermediario);

        config.PerfilCliente.Should().Be(PerfilCliente.ClientesExigentesComplexos);
        config.TipoProduto.Should().Be(TipoProduto.ProdutosSimples);
        config.VolumeNotas.Should().Be(VolumeNotas.MaisDe1000);
        config.NivelAutomacao.Should().Be(NivelAutomacao.Automatico);
        config.EmiteParaConsumidorFinal.Should().BeTrue();
        config.OperaIcmsSt.Should().BeTrue();
        config.NivelRelatorio.Should().Be(NivelRelatorio.Intermediario);
    }

    [Fact]
    public void Atualizar_DeveAtualizarConcluidoEmEUpdatedAt()
    {
        var config = ConfiguracaoEmpresa.Criar(
            EmpresaId,
            PerfilCliente.PequenasEmpresasSimples,
            TipoProduto.ServicosBasicos,
            VolumeNotas.Ate100,
            NivelAutomacao.Manual,
            consumidorFinal: false,
            operaSt: false,
            relatorio: NivelRelatorio.Basico);

        var concluidoOriginal = config.ConcluidoEm;

        config.Atualizar(
            PerfilCliente.ClientesExigentesComplexos,
            TipoProduto.ProdutosSimples,
            VolumeNotas.MaisDe1000,
            NivelAutomacao.Automatico,
            consumidorFinal: true,
            operaSt: true,
            relatorio: NivelRelatorio.Intermediario);

        config.ConcluidoEm.Should().NotBeNull();
        config.ConcluidoEm!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        config.ConcluidoEm.Should().BeOnOrAfter(concluidoOriginal!.Value);
        config.UpdatedAt.Should().NotBeNull();
        config.UpdatedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
