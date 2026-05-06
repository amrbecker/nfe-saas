using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class ContingenciaTests
{
    private static NotaFiscal CriarNota() =>
        NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);

    [Fact]
    public void NotaFiscal_PadraoEmissaoNormal()
    {
        var nota = CriarNota();
        nota.TipoEmissao.Should().Be(TipoEmissao.Normal);
    }

    [Fact]
    public void MarcarContingencia_SvcAn_AlteraTipoEmissao()
    {
        var nota = CriarNota();
        nota.MarcarContingencia(TipoEmissao.ContingenciaSvcAn);

        nota.TipoEmissao.Should().Be(TipoEmissao.ContingenciaSvcAn);
    }

    [Fact]
    public void MarcarContingencia_SvcRs_AlteraTipoEmissao()
    {
        var nota = CriarNota();
        nota.MarcarContingencia(TipoEmissao.ContingenciaSvcRs);

        nota.TipoEmissao.Should().Be(TipoEmissao.ContingenciaSvcRs);
    }

    [Fact]
    public void TipoEmissao_ValoresEnumCorretos()
    {
        // Validate NT-specified values for tpEmis
        ((int)TipoEmissao.Normal).Should().Be(1);
        ((int)TipoEmissao.ContingenciaSvcRs).Should().Be(6);
        ((int)TipoEmissao.ContingenciaSvcAn).Should().Be(9);
        ((int)TipoEmissao.ContingenciaFsda).Should().Be(5);
    }
}
