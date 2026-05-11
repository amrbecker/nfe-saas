using FluentAssertions;
using Moq;
using NfeSaas.Application.Commands.ClienteCommands;
using NfeSaas.Application.Commands.ProdutoCommands;
using NfeSaas.Application.Queries;
using NfeSaas.Application.Queries.ClienteQueries;
using NfeSaas.Application.Queries.ProdutoQueries;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Tests.Unit.Application;

public class QueryHandlerTests
{
    // ============================================================
    // GetNotasQuery
    // ============================================================
    [Fact]
    public async Task GetNotasQuery_DeveRetornarPaginadoComTotal()
    {
        var repo = new Mock<INotaFiscalRepository>();
        var empresaId = Guid.NewGuid();
        var notas = new List<NotaFiscal>
        {
            CriarNotaAutorizada(empresaId, 1),
            CriarNotaAutorizada(empresaId, 2)
        };
        repo.Setup(r => r.GetByEmpresaAsync(empresaId, 1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(notas);
        repo.Setup(r => r.CountByEmpresaAsync(empresaId, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var handler = new GetNotasQueryHandler(repo.Object);
        var result = await handler.Handle(new GetNotasQuery(empresaId), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Pagina.Should().Be(1);
        result.TamanhoPagina.Should().Be(20);
        result.Notas.Should().HaveCount(2);
    }

    // ============================================================
    // GetNotaDetalheQuery
    // ============================================================
    [Fact]
    public async Task GetNotaDetalheQuery_NotaExistente_RetornaDetalhes()
    {
        var repo = new Mock<INotaFiscalRepository>();
        var empresaId = Guid.NewGuid();
        var nota = CriarNotaAutorizada(empresaId, 1);
        repo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var handler = new GetNotaDetalheQueryHandler(repo.Object);
        var result = await handler.Handle(new GetNotaDetalheQuery(nota.Id, empresaId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(nota.Id);
        result.DentroPeriodoRetencao.Should().BeTrue();
        result.DataDescarteAutorizado.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNotaDetalheQuery_NotaDeOutraEmpresa_RetornaNull()
    {
        var repo = new Mock<INotaFiscalRepository>();
        var nota = CriarNotaAutorizada(Guid.NewGuid(), 1);
        repo.Setup(r => r.GetByIdAsync(nota.Id, It.IsAny<CancellationToken>())).ReturnsAsync(nota);

        var handler = new GetNotaDetalheQueryHandler(repo.Object);
        var result = await handler.Handle(
            new GetNotaDetalheQuery(nota.Id, Guid.NewGuid() /* outra empresa */),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNotaDetalheQuery_NotaInexistente_RetornaNull()
    {
        var repo = new Mock<INotaFiscalRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotaFiscal?)null);

        var handler = new GetNotaDetalheQueryHandler(repo.Object);
        var result = await handler.Handle(new GetNotaDetalheQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ============================================================
    // GetDashboardQuery
    // ============================================================
    [Fact]
    public async Task GetDashboardQuery_DeveCalcularTotaisCorretamente()
    {
        var repo = new Mock<INotaFiscalRepository>();
        var empresaId = Guid.NewGuid();

        var n1 = CriarNotaAutorizada(empresaId, 1, valor: 100m);
        var n2 = CriarNotaAutorizada(empresaId, 2, valor: 200m);

        repo.Setup(r => r.GetTotalEmitidoMesAsync(empresaId, 2026, 5, It.IsAny<CancellationToken>())).ReturnsAsync(300m);
        repo.Setup(r => r.GetContagemPorSituacaoAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<SituacaoNota, int>
            {
                [SituacaoNota.Autorizada] = 2,
                [SituacaoNota.Cancelada] = 1,
                [SituacaoNota.Rascunho] = 3,
            });
        repo.Setup(r => r.GetByPeriodoAsync(empresaId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { n1, n2 });

        var handler = new GetDashboardQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDashboardQuery(empresaId, 2026, 5), CancellationToken.None);

        result.TotalEmitidoMes.Should().Be(300m);
        result.TotalNotasAutorizadas.Should().Be(2);
        result.TotalNotasCanceladas.Should().Be(1);
        result.TotalNotasPendentes.Should().Be(3);  // só Rascunho (não tem Enviada)
        result.TotalNotasEmitidas.Should().Be(6);
    }

    // ============================================================
    // ProdutoQueries
    // ============================================================
    [Fact]
    public async Task GetProdutosQuery_DeveRetornarListaResumida()
    {
        var repo = new Mock<IProdutoRepository>();
        var empresaId = Guid.NewGuid();
        var produtos = new[]
        {
            Produto.Criar(empresaId, "P1", "Item 1", "12345678", "5102", "UN", OrigemMercadoria.Nacional, 10m),
            Produto.Criar(empresaId, "P2", "Item 2", "12345678", "5102", "UN", OrigemMercadoria.Nacional, 20m),
        };
        repo.Setup(r => r.GetByEmpresaAsync(empresaId, false, It.IsAny<CancellationToken>())).ReturnsAsync(produtos);

        var handler = new GetProdutosQueryHandler(repo.Object);
        var result = await handler.Handle(new GetProdutosQuery(empresaId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Codigo.Should().Be("P1");
    }

    [Fact]
    public async Task GetProdutoQuery_NaoEncontrado_RetornaNull()
    {
        var repo = new Mock<IProdutoRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Produto?)null);

        var handler = new GetProdutoQueryHandler(repo.Object);
        var result = await handler.Handle(new GetProdutoQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProdutoQuery_DeOutraEmpresa_RetornaNull()
    {
        var repo = new Mock<IProdutoRepository>();
        var produto = Produto.Criar(Guid.NewGuid(), "P1", "X", "12345678", "5102", "UN", OrigemMercadoria.Nacional, 1m);
        repo.Setup(r => r.GetByIdAsync(produto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(produto);

        var handler = new GetProdutoQueryHandler(repo.Object);
        var result = await handler.Handle(new GetProdutoQuery(Guid.NewGuid() /* outra */, produto.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProdutoQuery_DaMesmaEmpresa_RetornaDetalhe()
    {
        var repo = new Mock<IProdutoRepository>();
        var empresaId = Guid.NewGuid();
        var produto = Produto.Criar(empresaId, "P1", "Descrição", "12345678", "5102", "UN", OrigemMercadoria.Nacional, 99m);
        repo.Setup(r => r.GetByIdAsync(produto.Id, It.IsAny<CancellationToken>())).ReturnsAsync(produto);

        var handler = new GetProdutoQueryHandler(repo.Object);
        var result = await handler.Handle(new GetProdutoQuery(empresaId, produto.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Codigo.Should().Be("P1");
        result.ValorUnitarioPadrao.Should().Be(99m);
    }

    // ============================================================
    // ClienteQueries
    // ============================================================
    [Fact]
    public async Task GetClientesQuery_DeveRetornarListaResumida()
    {
        var repo = new Mock<IClienteRepository>();
        var empresaId = Guid.NewGuid();
        var clientes = new[]
        {
            CriarCliente(empresaId, "11122233344", "João"),
            CriarCliente(empresaId, "55566677700", "Maria")
        };
        repo.Setup(r => r.GetByEmpresaAsync(empresaId, false, It.IsAny<CancellationToken>())).ReturnsAsync(clientes);

        var handler = new GetClientesQueryHandler(repo.Object);
        var result = await handler.Handle(new GetClientesQuery(empresaId), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetClienteQuery_NaoEncontrado_RetornaNull()
    {
        var repo = new Mock<IClienteRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Cliente?)null);

        var handler = new GetClienteQueryHandler(repo.Object);
        var result = await handler.Handle(new GetClienteQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClienteQuery_DaMesmaEmpresa_RetornaDetalhe()
    {
        var repo = new Mock<IClienteRepository>();
        var empresaId = Guid.NewGuid();
        var c = CriarCliente(empresaId, "12345678901", "João");
        repo.Setup(r => r.GetByIdAsync(c.Id, It.IsAny<CancellationToken>())).ReturnsAsync(c);

        var handler = new GetClienteQueryHandler(repo.Object);
        var result = await handler.Handle(new GetClienteQuery(empresaId, c.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.RazaoSocial.Should().Be("João");
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static NotaFiscal CriarNotaAutorizada(Guid empresaId, int numero, decimal valor = 100m)
    {
        var n = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, numero,
            FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        var item = ItemNotaFiscal.Criar(n.Id, 1, "P1", "Item", "12345678", "5102", "UN", 1m, valor);
        n.AdicionarItem(item);
        n.MarcarEnviada("<x/>");
        n.Autorizar($"CHAVE{numero}", "PROT", "<r/>");
        return n;
    }

    private static Cliente CriarCliente(Guid empresaId, string cpfCnpj, string razao) =>
        Cliente.Criar(empresaId, TipoPessoa.PessoaFisica, cpfCnpj,
            razao, null, null, null, "Rua", "1", null, "Bairro", "Cidade",
            "SP", "01310100", "3550308", null, IndicadorIeDestinatario.NaoContribuinte);
}
