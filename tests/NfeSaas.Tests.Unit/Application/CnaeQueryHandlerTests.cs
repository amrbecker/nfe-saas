using FluentAssertions;
using Moq;
using NfeSaas.Application.Queries.CnaeQueries;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class CnaeQueryHandlerTests
{
    // ============================================================
    // BuscarCnaeQuery
    // ============================================================
    [Fact]
    public async Task BuscarCnae_TermoVazio_RetornaListaVazia()
    {
        var repo = new Mock<ICnaeRepository>();
        var handler = new BuscarCnaeQueryHandler(repo.Object);

        var result = await handler.Handle(new BuscarCnaeQuery(""), CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuscarCnae_ComTermo_RetornaResultadosMapeados()
    {
        var repo = new Mock<ICnaeRepository>();
        var cnaes = new[]
        {
            Cnae.Criar("6920601", "Atividades de contabilidade", "M", "69"),
            Cnae.Criar("6920602", "Atividades de consultoria e auditoria contábil e tributária", "M", "69")
        };
        repo.Setup(r => r.BuscarAsync("contab", 10, It.IsAny<CancellationToken>())).ReturnsAsync(cnaes);

        var handler = new BuscarCnaeQueryHandler(repo.Object);
        var result = await handler.Handle(new BuscarCnaeQuery("contab"), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Codigo.Should().Be("6920601");
        result[0].Secao.Should().Be("M");
        result[1].Codigo.Should().Be("6920602");
    }

    [Theory]
    [InlineData(0, 1)]      // limite menor que 1 → coerced para 1
    [InlineData(-5, 1)]
    [InlineData(100, 50)]   // limite acima de 50 → coerced para 50
    [InlineData(10, 10)]
    public async Task BuscarCnae_LimiteEhClampedEntre1E50(int solicitado, int esperado)
    {
        var repo = new Mock<ICnaeRepository>();
        repo.Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Cnae>());

        var handler = new BuscarCnaeQueryHandler(repo.Object);
        await handler.Handle(new BuscarCnaeQuery("teste", solicitado), CancellationToken.None);

        repo.Verify(r => r.BuscarAsync("teste", esperado, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============================================================
    // ValidarCnaeQuery
    // ============================================================
    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678")] // 8 dígitos
    [InlineData("abcdefg")]  // sem dígitos
    public async Task ValidarCnae_CodigoComFormatoInvalido_RetornaErro(string codigo)
    {
        var repo = new Mock<ICnaeRepository>();
        var handler = new ValidarCnaeQueryHandler(repo.Object);

        var result = await handler.Handle(new ValidarCnaeQuery(codigo), CancellationToken.None);

        result.Existe.Should().BeFalse();
        result.Cnae.Should().BeNull();
        result.MensagemErro.Should().Be("CNAE deve ter 7 dígitos.");
    }

    [Fact]
    public async Task ValidarCnae_CodigoNaoEncontrado_RetornaMensagemComCodigo()
    {
        var repo = new Mock<ICnaeRepository>();
        repo.Setup(r => r.GetByCodigoAsync("1234567", It.IsAny<CancellationToken>())).ReturnsAsync((Cnae?)null);

        var handler = new ValidarCnaeQueryHandler(repo.Object);
        var result = await handler.Handle(new ValidarCnaeQuery("1234567"), CancellationToken.None);

        result.Existe.Should().BeFalse();
        result.MensagemErro.Should().Contain("1234567");
    }

    [Fact]
    public async Task ValidarCnae_ComMascara_NormalizaDigitos()
    {
        // Usuário pode enviar com máscara: 6920-6/01
        var repo = new Mock<ICnaeRepository>();
        var cnae = Cnae.Criar("6920601", "Atividades de contabilidade");
        repo.Setup(r => r.GetByCodigoAsync("6920601", It.IsAny<CancellationToken>())).ReturnsAsync(cnae);

        var handler = new ValidarCnaeQueryHandler(repo.Object);
        var result = await handler.Handle(new ValidarCnaeQuery("6920-6/01"), CancellationToken.None);

        result.Existe.Should().BeTrue();
        result.Cnae!.Codigo.Should().Be("6920601");
    }
}
