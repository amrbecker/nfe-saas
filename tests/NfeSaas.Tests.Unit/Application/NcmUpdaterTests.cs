using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NfeSaas.Application.Services;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class NcmUpdaterTests
{
    private const string JsonComUmNcm = """
    {
      "Data_Ultima_Atualizacao_NCM": "Vigente em 11/05/2026",
      "Ato": "Resolução Gecex nº 812/2025",
      "Nomenclaturas": [
        { "Codigo": "0101.21.00", "Descricao": "-- Reprodutores de raça pura", "Data_Fim": "31/12/9999" },
        { "Codigo": "01.01",      "Descricao": "Cavalos, asininos e muares",   "Data_Fim": "31/12/9999" }
      ]
    }
    """;

    [Fact]
    public async Task Atualizar_SemFonte_RetornaFalha()
    {
        var sut = Criar(out _, out _, out _);
        var result = await sut.AtualizarAsync(new NcmUpdateOptions(null, null));

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("URL ou arquivo local");
    }

    [Fact]
    public async Task Atualizar_ArquivoLocal_FazUpsertEPersisteSaveChanges()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, JsonComUmNcm);

        try
        {
            var sut = Criar(out var repo, out var uow, out _);
            repo.Setup(r => r.GetVersaoTabelaAtualAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

            var result = await sut.AtualizarAsync(new NcmUpdateOptions(null, tempFile));

            result.Sucesso.Should().BeTrue();
            result.VersaoNova.Should().Be("Resolução Gecex nº 812/2025");
            result.TotalInseridosOuAtualizados.Should().Be(1);
            repo.Verify(r => r.UpsertManyAsync(
                It.Is<IEnumerable<Ncm>>(l => l.Count() == 1),
                "Resolução Gecex nº 812/2025",
                It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Atualizar_MesmaVersao_CurtoCircuita_NaoChamaUpsert()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, JsonComUmNcm);

        try
        {
            var sut = Criar(out var repo, out var uow, out _);
            repo.Setup(r => r.GetVersaoTabelaAtualAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("Resolução Gecex nº 812/2025");

            var result = await sut.AtualizarAsync(new NcmUpdateOptions(null, tempFile));

            result.Sucesso.Should().BeTrue();
            result.TotalInseridosOuAtualizados.Should().Be(0);
            repo.Verify(r => r.UpsertManyAsync(It.IsAny<IEnumerable<Ncm>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Atualizar_FalhaHttp_RetornaErroSemPropagar()
    {
        var sut = CriarComHttpHandler(JsonComUmNcm, HttpStatusCode.InternalServerError, out _, out _);

        var result = await sut.AtualizarAsync(new NcmUpdateOptions("https://exemplo.com/ncm.json"));

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("Falha ao obter JSON");
    }

    [Fact]
    public async Task Atualizar_HttpRetornaJsonValido_FazUpsert()
    {
        var sut = CriarComHttpHandler(JsonComUmNcm, HttpStatusCode.OK, out var repo, out _);
        repo.Setup(r => r.GetVersaoTabelaAtualAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        var result = await sut.AtualizarAsync(new NcmUpdateOptions("https://exemplo.com/ncm.json"));

        result.Sucesso.Should().BeTrue();
        result.TotalInseridosOuAtualizados.Should().Be(1);
        repo.Verify(r => r.UpsertManyAsync(It.IsAny<IEnumerable<Ncm>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Atualizar_JsonInvalido_RetornaFalhaNoParser()
    {
        var sut = CriarComHttpHandler("{ não é json válido", HttpStatusCode.OK, out _, out _);

        var result = await sut.AtualizarAsync(new NcmUpdateOptions("https://exemplo.com/ncm.json"));

        result.Sucesso.Should().BeFalse();
        result.MensagemErro.Should().Contain("parsear");
    }

    [Fact]
    public async Task Atualizar_VersaoOverride_UsaNoLugarDaVersaoDoJson()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, JsonComUmNcm);

        try
        {
            var sut = Criar(out var repo, out _, out _);
            repo.Setup(r => r.GetVersaoTabelaAtualAsync(It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

            var result = await sut.AtualizarAsync(new NcmUpdateOptions(null, tempFile, "2026-05"));

            result.Sucesso.Should().BeTrue();
            result.VersaoNova.Should().Be("2026-05");
            repo.Verify(r => r.UpsertManyAsync(It.IsAny<IEnumerable<Ncm>>(), "2026-05", It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static NcmUpdater Criar(out Mock<INcmRepository> repo, out Mock<IUnitOfWork> uow, out IHttpClientFactory http)
    {
        repo = new Mock<INcmRepository>();
        uow = new Mock<IUnitOfWork>();
        http = new StubHttpClientFactory("", HttpStatusCode.OK);
        return new NcmUpdater(repo.Object, uow.Object, http, NullLogger<NcmUpdater>.Instance);
    }

    private static NcmUpdater CriarComHttpHandler(string body, HttpStatusCode status, out Mock<INcmRepository> repo, out Mock<IUnitOfWork> uow)
    {
        repo = new Mock<INcmRepository>();
        uow = new Mock<IUnitOfWork>();
        IHttpClientFactory http = new StubHttpClientFactory(body, status);
        return new NcmUpdater(repo.Object, uow.Object, http, NullLogger<NcmUpdater>.Instance);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly string _body;
        private readonly HttpStatusCode _status;
        public StubHttpClientFactory(string body, HttpStatusCode status) { _body = body; _status = status; }

        public HttpClient CreateClient(string name)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
            return new HttpClient(handler.Object);
        }
    }
}
