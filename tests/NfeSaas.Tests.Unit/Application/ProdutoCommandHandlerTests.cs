using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.ProdutoCommands;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class ProdutoCommandHandlerTests
{
    private readonly Mock<IProdutoRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CreateProdutoCommandHandler CreateHandler() =>
        new(_repo.Object, _uow.Object);

    private UpdateProdutoCommandHandler UpdateHandler() =>
        new(_repo.Object, _uow.Object);

    private static CreateProdutoDto Dto(
        string codigo = "P01", string ncm = "12345678", string cfop = "5102",
        decimal valor = 10m, string? cest = null, string? gtin = null, string? anp = null) =>
        new(codigo, "Produto Teste", ncm, cfop, "UN", 0, valor, cest, gtin, anp);

    [Fact]
    public async Task Create_DadosValidos_RetornaProdutoCriado()
    {
        var empresaId = Guid.NewGuid();
        _repo.Setup(r => r.GetByCodigoAsync(empresaId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Produto?)null);

        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(empresaId, Dto()), CancellationToken.None);

        result.Erro.Should().BeNull();
        result.Produto.Should().NotBeNull();
        result.Produto!.Codigo.Should().Be("P01");
        _repo.Verify(r => r.AddAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_NcmInvalido_RetornaErro()
    {
        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(Guid.NewGuid(), Dto(ncm: "1234")), CancellationToken.None);

        result.Produto.Should().BeNull();
        result.Erro.Should().Contain("NCM");
        _repo.Verify(r => r.AddAsync(It.IsAny<Produto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_CfopInvalido_RetornaErro()
    {
        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(Guid.NewGuid(), Dto(cfop: "9999")), CancellationToken.None);

        result.Produto.Should().BeNull();
        result.Erro.Should().Contain("CFOP");
    }

    [Fact]
    public async Task Create_CodigoJaExistente_RetornaConflito()
    {
        var empresaId = Guid.NewGuid();
        var existente = Produto.Criar(empresaId, "P01", "Já existe", "12345678",
            "5102", "UN", OrigemMercadoria.Nacional, 1m);
        _repo.Setup(r => r.GetByCodigoAsync(empresaId, "P01", It.IsAny<CancellationToken>()))
             .ReturnsAsync(existente);

        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(empresaId, Dto()), CancellationToken.None);

        result.Produto.Should().BeNull();
        result.Erro.Should().Contain("código");
    }

    [Fact]
    public async Task Create_ValorNegativo_RetornaErro()
    {
        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(Guid.NewGuid(), Dto(valor: -1m)), CancellationToken.None);

        result.Erro.Should().Contain("negativo");
    }

    [Fact]
    public async Task Create_GtinInvalido_RetornaErro()
    {
        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(Guid.NewGuid(), Dto(gtin: "1234567890123")), CancellationToken.None);

        result.Erro.Should().Contain("GTIN");
    }

    [Fact]
    public async Task Create_AnpComTamanhoErrado_RetornaErro()
    {
        var result = await CreateHandler().Handle(
            new CreateProdutoCommand(Guid.NewGuid(), Dto(anp: "12345")), CancellationToken.None);

        result.Erro.Should().Contain("ANP");
    }

    [Fact]
    public async Task Update_ProdutoNaoExiste_RetornaErro()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((Produto?)null);

        var dto = new UpdateProdutoDto("P01", "X", "12345678", "5102", "UN", 0, 1m, null, null, null);
        var result = await UpdateHandler().Handle(
            new UpdateProdutoCommand(Guid.NewGuid(), Guid.NewGuid(), dto), CancellationToken.None);

        result.Produto.Should().BeNull();
        result.Erro.Should().Contain("não encontrado");
    }

    [Fact]
    public async Task Update_DeOutraEmpresa_RetornaErro()
    {
        var produto = Produto.Criar(Guid.NewGuid(), "P01", "X", "12345678",
            "5102", "UN", OrigemMercadoria.Nacional, 1m);
        _repo.Setup(r => r.GetByIdAsync(produto.Id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(produto);

        var dto = new UpdateProdutoDto("P01", "X", "12345678", "5102", "UN", 0, 1m, null, null, null);
        var result = await UpdateHandler().Handle(
            new UpdateProdutoCommand(Guid.NewGuid() /* empresa diferente */, produto.Id, dto), CancellationToken.None);

        result.Produto.Should().BeNull();
        result.Erro.Should().Contain("não encontrado");
    }
}
