using FluentAssertions;
using Moq;
using NfeSaas.Application.Queries.NcmQueries;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class NcmQueryHandlerTests
{
    // ============================================================
    // BuscarNcmQuery
    // ============================================================
    [Fact]
    public async Task BuscarNcm_TermoVazio_RetornaListaVazia()
    {
        var repo = new Mock<INcmRepository>();
        var handler = new BuscarNcmQueryHandler(repo.Object);

        var result = await handler.Handle(new BuscarNcmQuery(""), CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuscarNcm_ComTermo_RetornaResultadosMapeados()
    {
        var repo = new Mock<INcmRepository>();
        var ncms = new[]
        {
            Ncm.Criar("85171231", "Telefones celulares (smartphones)", "2024-12", exigeCest: true),
            Ncm.Criar("85176294", "Modems e roteadores", "2024-12")
        };
        repo.Setup(r => r.BuscarAsync("8517", 10, It.IsAny<CancellationToken>())).ReturnsAsync(ncms);

        var handler = new BuscarNcmQueryHandler(repo.Object);
        var result = await handler.Handle(new BuscarNcmQuery("8517"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Codigo.Should().Be("85171231");
        result[0].ExigeCest.Should().BeTrue();
        result[1].Codigo.Should().Be("85176294");
    }

    [Theory]
    [InlineData(0, 1)]      // limite menor que 1 → coerced para 1
    [InlineData(-5, 1)]
    [InlineData(100, 50)]   // limite acima de 50 → coerced para 50
    [InlineData(10, 10)]
    public async Task BuscarNcm_LimiteEhClampedEntre1E50(int solicitado, int esperado)
    {
        var repo = new Mock<INcmRepository>();
        repo.Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Ncm>());

        var handler = new BuscarNcmQueryHandler(repo.Object);
        await handler.Handle(new BuscarNcmQuery("teste", solicitado), CancellationToken.None);

        repo.Verify(r => r.BuscarAsync("teste", esperado, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============================================================
    // ValidarNcmQuery
    // ============================================================
    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("123456789")] // 9 dígitos
    [InlineData("abcdefgh")]  // sem dígitos
    public async Task ValidarNcm_CodigoComFormatoInvalido_RetornaErro(string codigo)
    {
        var repo = new Mock<INcmRepository>();
        var handler = new ValidarNcmQueryHandler(repo.Object);

        var result = await handler.Handle(new ValidarNcmQuery(codigo), CancellationToken.None);

        result.Existe.Should().BeFalse();
        result.Ncm.Should().BeNull();
        result.MensagemErro.Should().Be("NCM deve ter 8 dígitos.");
    }

    [Fact]
    public async Task ValidarNcm_CodigoNaoEncontrado_RetornaMensagemComCodigo()
    {
        var repo = new Mock<INcmRepository>();
        repo.Setup(r => r.GetByCodigoAsync("12345678", It.IsAny<CancellationToken>())).ReturnsAsync((Ncm?)null);

        var handler = new ValidarNcmQueryHandler(repo.Object);
        var result = await handler.Handle(new ValidarNcmQuery("12345678"), CancellationToken.None);

        result.Existe.Should().BeFalse();
        result.MensagemErro.Should().Contain("12345678");
    }

    [Fact]
    public async Task ValidarNcm_ComMascara_NormalizaDigitos()
    {
        // Usuário pode enviar com pontos: 8517.12.31
        var repo = new Mock<INcmRepository>();
        var ncm = Ncm.Criar("85171231", "Telefones celulares", "2024-12");
        repo.Setup(r => r.GetByCodigoAsync("85171231", It.IsAny<CancellationToken>())).ReturnsAsync(ncm);

        var handler = new ValidarNcmQueryHandler(repo.Object);
        var result = await handler.Handle(new ValidarNcmQuery("8517.12.31"), CancellationToken.None);

        result.Existe.Should().BeTrue();
        result.Ncm!.Codigo.Should().Be("85171231");
    }

    // ============================================================
    // GetNcmStatusQuery
    // ============================================================
    [Fact]
    public async Task GetNcmStatus_RetornaTotalEVersao()
    {
        var repo = new Mock<INcmRepository>();
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(14523);
        repo.Setup(r => r.GetVersaoTabelaAtualAsync(It.IsAny<CancellationToken>())).ReturnsAsync("2024-12");

        var handler = new GetNcmStatusQueryHandler(repo.Object);
        var result = await handler.Handle(new GetNcmStatusQuery(), CancellationToken.None);

        result.TotalAtivos.Should().Be(14523);
        result.VersaoTabela.Should().Be("2024-12");
    }
}
