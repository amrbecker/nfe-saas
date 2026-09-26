using FluentAssertions;
using Moq;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Chamados;
using NfeSaas.Application.Assistente.Curadoria;
using NfeSaas.Application.Assistente.Monitoramento;
using NfeSaas.Application.Assistente.Servicos;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P7;

public class CuradoriaEChamadosTests
{
    private const string HtmlPortal = """
        <html><head><script>var x = "Nota Técnica falsa no script";</script><style>.a{}</style></head>
        <body><div class="menu"><a href="/">Início</a></div>
        <ul><li>Nota Técnica 2026.009 v.1.00 - Publicada em 09/09/2026</li>
        <li><a href="#">Implantadas NTs 2026.002 em PRODU&#199;&#195;O na SVRS</a></li>
        <li>Nota Técnica 2026.009 v.1.00 - Publicada em 09/09/2026</li>
        <li>Fale conosco</li></ul></body></html>
        """;

    [Fact]
    public void Extrator_pega_so_itens_relevantes_decodifica_e_deduplica()
    {
        var itens = ExtratorPublicacoes.Extrair(HtmlPortal);

        itens.Select(i => i.Titulo).Should().BeEquivalentTo(new[]
        {
            "Nota Técnica 2026.009 v.1.00 - Publicada em 09/09/2026",
            "Implantadas NTs 2026.002 em PRODUÇÃO na SVRS"
        });
        itens.Should().NotContain(i => i.Titulo.Contains("falsa"));
    }

    [Fact]
    public void Hash_do_conjunto_nao_depende_da_ordem()
    {
        var a = ExtratorPublicacoes.Extrair(HtmlPortal);
        ExtratorPublicacoes.HashConjunto(a).Should().Be(ExtratorPublicacoes.HashConjunto(a.AsEnumerable().Reverse()));
    }

    [Fact]
    public void Classificacao_da_ia_e_interpretada_com_seguranca()
    {
        var ids = new HashSet<string> { "preenchimento/ibs-cbs-cclasstrib" };
        var c = ClassificadorPublicacoes.Interpretar(
            "Aqui está: {\"relevante\":true,\"resumo\":\"Nova regra de IBS/CBS\",\"vigencia_inicio\":\"2026-10-01\"," +
            "\"artigos_afetados\":[\"preenchimento/ibs-cbs-cclasstrib\",\"inventado/x\"],\"impacto_tecnico\":true}", ids);

        c!.Relevante.Should().BeTrue();
        c.ArtigosAfetados.Should().Equal("preenchimento/ibs-cbs-cclasstrib");
        c.VigenciaInicio.Should().Be(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        ClassificadorPublicacoes.Interpretar("sem json", ids).Should().BeNull();
    }

    [Fact]
    public void Primeira_leitura_nao_marca_mudanca_e_tres_falhas_deixam_a_fonte_cega()
    {
        var f = FonteMonitorada.Criar("Portal", NivelFonte.N1NormaOficial, "https://x", MecanismoMonitoramento.HashPagina, 24);
        f.RegistrarLeitura("a", DateTime.UtcNow).Should().BeFalse();
        f.RegistrarLeitura("b", DateTime.UtcNow).Should().BeTrue();
        f.RegistrarFalha(DateTime.UtcNow); f.RegistrarFalha(DateTime.UtcNow);
        f.Cega.Should().BeFalse();
        f.RegistrarFalha(DateTime.UtcNow);
        f.Cega.Should().BeTrue();
    }

    [Fact]
    public async Task Lacunas_sao_agrupadas_sem_identificar_escritorio()
    {
        var sinais = new Mock<ISinalProdutoRepository>();
        sinais.Setup(s => s.ListarAsync(TipoSinalProduto.LacunaConhecimento, It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SinalProduto>
            {
                SinalProduto.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoSinalProduto.LacunaConhecimento, "cClassTrib na devolução?", "emitir-nfe", "cclasstrib"),
                SinalProduto.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TipoSinalProduto.LacunaConhecimento, "Qual cClassTrib usar?", "emitir-nfe", "cclasstrib"),
                SinalProduto.Criar(null, null, null, TipoSinalProduto.LacunaConhecimento, "Prazo de CC-e?", null, null),
            });
        var h = new CuradoriaHandlers(Mock.Of<IPublicacaoDetectadaRepository>(), Mock.Of<IFonteMonitoradaRepository>(),
            sinais.Object, Mock.Of<IBaseConhecimento>(), Mock.Of<IUnitOfWork>());

        var lacunas = await h.Handle(new LacunasQuery(30), default);

        lacunas[0].Should().BeEquivalentTo(new LacunaAgrupadaDto("cclasstrib", 2, new() { "cClassTrib na devolução?", "Qual cClassTrib usar?" }));
        typeof(LacunaAgrupadaDto).GetProperties().Select(p => p.Name).Should().NotContain(n => n.Contains("Escritorio") || n.Contains("Empresa"));
    }

    [Fact]
    public async Task Chamado_e_sinal_sao_gravados_sanitizados()
    {
        var chamados = new Mock<IChamadoRepository>();
        Chamado? gravado = null;
        chamados.Setup(c => c.AddAsync(It.IsAny<Chamado>(), It.IsAny<CancellationToken>())).Callback<Chamado, CancellationToken>((c, _) => gravado = c);
        var notas = new Mock<INotaFiscalRepository>();
        var notaAlheia = NotaFiscal.Criar(Guid.NewGuid(), TipoNota.NFe, 1, 1, FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        notas.Setup(n => n.GetByIdAsync(notaAlheia.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notaAlheia);

        var h = new CriarChamadoHandler(chamados.Object, notas.Object, new SanitizadorIA(), Mock.Of<IUnitOfWork>());
        await h.Handle(new CriarChamadoCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new CriarChamadoDto(
            "Erro ao emitir para 529.982.247-25", "O cliente joao@exemplo.com não recebe", null, SeveridadeChamado.Alta, notaAlheia.Id,
            new ContextoTelaDto("emitir-nfe", Rastro: new() { "digitou 529.982.247-25" }, SentryEventId: "abc123"))), default);

        gravado!.Titulo.Should().NotContain("529.982.247-25").And.Contain("[CPF_1]");
        gravado.Descricao.Should().NotContain("joao@exemplo.com");
        gravado.ContextoTecnicoJson.Should().NotContain("529.982.247-25").And.Contain("abc123");
        gravado.NotaFiscalId.Should().BeNull("a nota é de outra empresa");

        var sinais = new Mock<ISinalProdutoRepository>();
        SinalProduto? sinal = null;
        sinais.Setup(s => s.AddAsync(It.IsAny<SinalProduto>(), It.IsAny<CancellationToken>())).Callback<SinalProduto, CancellationToken>((s, _) => sinal = s);
        await new RegistrarSinalHandler(sinais.Object, new SanitizadorIA(), Mock.Of<IUnitOfWork>())
            .Handle(new RegistrarSinalCommand(null, null, null, TipoSinalProduto.Dor, "Liga pra mim (11) 98765-4321", null, "  CFOP "), default);
        sinal!.ResumoSanitizado.Should().NotContain("98765");
        sinal.Tema.Should().Be("cfop");
    }
}
