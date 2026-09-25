using FluentAssertions;
using Moq;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Automacoes;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P5;

public class AutomacoesHandlersTests
{
    private readonly Guid _empresa = Guid.NewGuid();
    private readonly Mock<INotaFiscalRepository> _notas = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IProdutoRepository> _produtos = new();
    private readonly Mock<IConsultasAssistenteRepository> _consultas = new();

    private AutomacoesHandlers Handlers() => new(_notas.Object, _clientes.Object, _produtos.Object, _consultas.Object);

    private NotaFiscal Nota(Guid empresaId, SituacaoNota situacao = SituacaoNota.Autorizada)
    {
        var n = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, 77, FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.SetDestinatario("11222333000181", "Cliente Exemplo Ltda", "c@exemplo.com", TipoPessoa.PessoaJuridica,
            "Rua X", "1", "Centro", "BH", "MG", "30110000", "3106200", "0010000000001");
        var item = ItemNotaFiscal.Criar(n.Id, 1, "CANECA-300", "Caneca 300 ml", "69120000", "6102", "UN", 10, 25m);
        n.AdicionarItem(item);
        if (situacao == SituacaoNota.Autorizada) { n.MarcarEnviada("<x/>"); n.Autorizar("CHAVE", "PROT", "<r/>"); }
        _notas.Setup(r => r.GetByIdAsync(n.Id, It.IsAny<CancellationToken>())).ReturnsAsync(n);
        return n;
    }

    [Fact]
    public async Task Preparar_nota_copia_destinatario_e_itens_sem_idempotencia()
    {
        var n = Nota(_empresa);
        var dto = await Handlers().Handle(new PrepararNotaQuery(_empresa, n.Id), default);

        dto.Should().NotBeNull();
        dto!.Destinatario.CpfCnpj.Should().Be("11222333000181");
        dto.Itens.Should().ContainSingle(i => i.CodigoProduto == "CANECA-300" && i.Ncm == "69120000" && i.Cfop == "6102");
        dto.IdempotencyKey.Should().BeNull();
    }

    [Fact]
    public async Task Automacoes_nao_enxergam_nota_de_outra_empresa()
    {
        var alheia = Nota(Guid.NewGuid());
        (await Handlers().Handle(new PrepararNotaQuery(_empresa, alheia.Id), default)).Should().BeNull();
        (await Handlers().Handle(new CadastroSugeridoQuery(_empresa, alheia.Id), default)).Should().BeNull();
    }

    [Fact]
    public async Task Cadastro_sugerido_aponta_destinatario_e_produto_digitados_a_mao()
    {
        var n = Nota(_empresa);
        _consultas.Setup(c => c.CodigosProdutosExistentesAsync(_empresa, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        var dto = await Handlers().Handle(new CadastroSugeridoQuery(_empresa, n.Id), default);

        dto!.DestinatarioNovo.Should().BeTrue();
        dto.Destinatario!.RazaoSocial.Should().Be("Cliente Exemplo Ltda");
        dto.ProdutosNovos.Should().ContainSingle(p => p.Codigo == "CANECA-300");
    }

    [Fact]
    public async Task Cadastro_sugerido_aponta_divergencia_de_ncm_com_o_cadastro()
    {
        var n = Nota(_empresa);
        _consultas.Setup(c => c.CodigosProdutosExistentesAsync(_empresa, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { "CANECA-300" });
        var produto = Produto.Criar(_empresa, "CANECA-300", "Caneca", "69111010", "5102", "UN", OrigemMercadoria.Nacional, 25m);
        _produtos.Setup(p => p.GetByCodigoAsync(_empresa, "CANECA-300", It.IsAny<CancellationToken>())).ReturnsAsync(produto);
        _clientes.Setup(c => c.GetByCpfCnpjAsync(_empresa, "11222333000181", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Cliente.Criar(_empresa, TipoPessoa.PessoaJuridica, "11222333000181", "Cliente Exemplo Ltda", null, null, null,
                "Rua X", "1", null, "Centro", "BH", "MG", "30110000", "3106200", null, IndicadorIeDestinatario.NaoContribuinte));

        var dto = await Handlers().Handle(new CadastroSugeridoQuery(_empresa, n.Id), default);

        dto!.DestinatarioNovo.Should().BeFalse();
        dto.Divergencias.Should().Contain(d => d.Campo == "ncm" && d.ValorCadastro == "69111010" && d.ValorNota == "69120000");
        dto.Divergencias.Should().Contain(d => d.Campo == "cfop");
    }

    private static NotaResumoAssistente Resumo(string doc, string cfop, int csosn, int dias) =>
        new(Guid.NewGuid(), Guid.Empty, TipoNota.NFe, 1, doc, "SP", SituacaoNota.Autorizada, AmbienteSefaz.Producao,
            DateTime.UtcNow.AddDays(-dias), null, new[] { new ItemResumoAssistente(1, "P1", "Produto", "69120000", null, cfop, 0, csosn, "UN") });

    [Fact]
    public async Task Padroes_so_aparecem_com_80_por_cento_das_ultimas_notas()
    {
        var notas = Enumerable.Range(1, 9).Select(i => Resumo("11222333000181", "5102", 102, i))
            .Append(Resumo("11222333000181", "5405", 500, 10)).ToList();
        _consultas.Setup(c => c.NotasAsync(_empresa, It.IsAny<DateTime>(), SituacaoNota.Autorizada, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notas);

        var r = await Handlers().Handle(new GetPadroesPreenchimentoQuery(_empresa, "11.222.333/0001-81", "P1"), default);

        r.NotasAnalisadas.Should().Be(10);
        r.Padroes.Should().ContainSingle();
        r.Padroes[0].Should().Match<NfeSaas.Application.DTOs.Assistente.PadraoItemDto>(p => p.Cfop == "5102" && p.CstCsosn == "102" && p.Frequencia == 9 && p.Total == 10);
    }

    [Fact]
    public async Task Sem_maioria_clara_nao_ha_padrao()
    {
        var notas = Enumerable.Range(1, 5).Select(i => Resumo("11222333000181", "5102", 102, i))
            .Concat(Enumerable.Range(6, 5).Select(i => Resumo("11222333000181", "5405", 500, i))).ToList();
        _consultas.Setup(c => c.NotasAsync(_empresa, It.IsAny<DateTime>(), SituacaoNota.Autorizada, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notas);

        (await Handlers().Handle(new GetPadroesPreenchimentoQuery(_empresa, "11222333000181", "P1"), default)).Padroes.Should().BeEmpty();
    }
}
