using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NfeSaas.Application.Assistente;
using NfeSaas.Application.Assistente.Ia;
using NfeSaas.Application.Assistente.Servicos;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;
using NfeSaas.Infrastructure.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P3;

public class ServicoRespostaOriTests
{
    private readonly Guid _empresaId = Guid.NewGuid();
    private readonly Guid _escritorioId = Guid.NewGuid();
    private readonly Guid _usuarioId = Guid.NewGuid();
    private readonly Mock<IAssistenteIA> _ia = new();
    private readonly Mock<INotaFiscalRepository> _notas = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IInteracaoAssistenteRepository> _interacoes = new();
    private readonly Mock<ICotaAssistente> _cota = new();
    private readonly List<IReadOnlyList<MensagemIa>> _mensagensEnviadas = new();
    private readonly Queue<string> _respostasModelo = new();
    private readonly AssistenteOptions _opcoes = new();

    private static readonly ArtigoKb ArtigoNcm = new("rejeicoes/ncm", "NCM inexistente", "rejeicoes", NivelFonte.N1NormaOficial,
        new[] { "778" }, new[] { "ncm" }, new[] { "emitir-nfe" }, new[] { new FonteArtigo("MOC", null, NivelFonte.N1NormaOficial) },
        null, null, null, null, null, "publicado", "O NCM informado não existe na tabela vigente.",
        "Corrija o NCM do item no cadastro do produto ou na tela Emitir NF-e.");

    private EscopoOri Escopo => new(_usuarioId, _escritorioId, _empresaId, "User");

    public ServicoRespostaOriTests()
    {
        _ia.Setup(i => i.EstaHabilitado(RotaIa.Conversa)).Returns(true);
        _ia.Setup(i => i.CustoEstimadoUsd(It.IsAny<RotaIa>(), It.IsAny<UsoIa>())).Returns(0.001m);
        _ia.Setup(i => i.CompletarAsync(RotaIa.Conversa, It.IsAny<IReadOnlyList<MensagemIa>>(), It.IsAny<OpcoesIa>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RotaIa _, IReadOnlyList<MensagemIa> m, OpcoesIa _, CancellationToken _) =>
            {
                _mensagensEnviadas.Add(m);
                return new RespostaIa(_respostasModelo.Dequeue(), new UsoIa(1000, 800, 200), "deepseek-flash", Array.Empty<ChamadaFerramentaIa>());
            });
        _cota.Setup(c => c.ObterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusCotaDto(true, 1, 40, 1, 150, null));
    }

    private (ServicoRespostaOri Servico, MontadorContexto Contexto, FerramentasOri Ferramentas) Criar()
    {
        var kb = new BaseConhecimento(Options.Create(_opcoes), () => new[] { ArtigoNcm });
        var sanitizador = new SanitizadorIA();
        var empresas = new Mock<IEmpresaRepository>();
        var contexto = new MontadorContexto(empresas.Object, Mock.Of<IEscritorioRepository>(),
            Mock.Of<IConfiguracaoEmpresaRepository>(), _notas.Object, sanitizador, kb);
        var ferramentas = new FerramentasOri(_notas.Object, empresas.Object, Mock.Of<IEscritorioRepository>(),
            Mock.Of<INcmRepository>(), Mock.Of<ISefazService>(), kb, sanitizador, contexto, _mediator.Object,
            NullLogger<FerramentasOri>.Instance);
        var servico = new ServicoRespostaOri(_ia.Object, kb, sanitizador, _cota.Object,
            new MontadorPrompt(new PromptSistemaEmbutido()), contexto, ferramentas, _interacoes.Object,
            Mock.Of<IUnitOfWork>(), _mediator.Object, Options.Create(_opcoes), NullLogger<ServicoRespostaOri>.Instance);
        return (servico, contexto, ferramentas);
    }

    private NotaFiscal NotaRejeitada(Guid empresaId)
    {
        var n = NotaFiscal.Criar(empresaId, TipoNota.NFe, 1, 1482, FinalidadeNota.Normal, TipoOperacao.Saida, AmbienteSefaz.Homologacao);
        n.SetDestinatario("52998224725", "Maria Fictícia", "maria@exemplo.com", TipoPessoa.PessoaFisica,
            "Rua X", "1", "Bairro", "Cidade", "MG", "30110000", "3106200", null);
        n.Rejeitar("[778] Rejeição: Informado NCM inexistente");
        _notas.Setup(r => r.GetByIdAsync(n.Id, It.IsAny<CancellationToken>())).ReturnsAsync(n);
        return n;
    }

    [Fact]
    public async Task Sem_ia_responde_so_com_o_artigo_da_base()
    {
        _ia.Setup(i => i.EstaHabilitado(RotaIa.Conversa)).Returns(false);
        var nota = NotaRejeitada(_empresaId);
        var (servico, _, _) = Criar();

        var r = await servico.ResponderAsync(new PedidoRespostaOri(Escopo, TipoInteracaoAssistente.ExplicarRejeicao, "por quê?", null, nota.Id), null, default);

        r.RespondidaSemModelo.Should().BeTrue();
        r.Texto.Should().Contain(ArtigoNcm.ResumoCurto);
        r.Selo.Should().Be(NivelFonte.N1NormaOficial);
        r.Citacoes.Should().ContainSingle(c => c.Id == "rejeicoes/ncm");
        _ia.Verify(i => i.CompletarAsync(It.IsAny<RotaIa>(), It.IsAny<IReadOnlyList<MensagemIa>>(), It.IsAny<OpcoesIa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cota_esgotada_nao_chama_o_modelo()
    {
        _cota.Setup(c => c.ObterAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StatusCotaDto(false, 40, 40, 40, 150, "Limite diário"));
        var (servico, _, _) = Criar();

        var r = await servico.ResponderAsync(new PedidoRespostaOri(Escopo, TipoInteracaoAssistente.PerguntaLivre, "ncm inexistente", null), null, default);

        r.RespondidaSemModelo.Should().BeTrue();
        r.Cota!.Permitido.Should().BeFalse();
    }

    [Fact]
    public async Task Resposta_fundamentada_e_aprovada_com_selo_e_registrada()
    {
        var nota = NotaRejeitada(_empresaId);
        _respostasModelo.Enqueue("**O que fazer** Corrija o NCM do item na tela Emitir NF-e (nota 1482). [fonte: rejeicoes/ncm]");
        var (servico, _, _) = Criar();

        var r = await servico.ResponderAsync(new PedidoRespostaOri(Escopo, TipoInteracaoAssistente.ExplicarRejeicao, "por quê?", null, nota.Id), null, default);

        r.VerificacaoFalhou.Should().BeFalse();
        r.Selo.Should().Be(NivelFonte.N1NormaOficial);
        r.InteracaoId.Should().NotBeNull();
        _interacoes.Verify(i => i.AddAsync(It.Is<InteracaoAssistente>(x => x.ArtigosCitados == "rejeicoes/ncm"), It.IsAny<CancellationToken>()), Times.Once);
        _cota.Verify(c => c.RegistrarAsync(_usuarioId, _escritorioId, It.IsAny<UsoIa>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Numero_sem_fonte_gera_nova_tentativa_e_depois_resposta_N4()
    {
        _respostasModelo.Enqueue("O prazo é de 99 dias. [fonte: rejeicoes/ncm]");
        _respostasModelo.Enqueue("Continua sendo 99 dias.");
        var (servico, _, _) = Criar();

        var r = await servico.ResponderAsync(new PedidoRespostaOri(Escopo, TipoInteracaoAssistente.PerguntaLivre, "qual o prazo?", null), null, default);

        _mensagensEnviadas.Should().HaveCount(2);
        _mensagensEnviadas[1][^1].Texto.Should().Contain("99");
        r.VerificacaoFalhou.Should().BeTrue();
        r.Selo.Should().Be(NivelFonte.N4SemFonte);
        r.Texto.Should().Be(VerificadorResposta.RespostaSemFonte);
        _mediator.Verify(m => m.Send(It.Is<RegistrarSinalCommand>(s => s.Tipo == TipoSinalProduto.LacunaConhecimento), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dados_pessoais_nao_chegam_ao_modelo_e_voltam_reidratados()
    {
        var nota = NotaRejeitada(_empresaId);
        _respostasModelo.Enqueue("Para o [CPF_1], corrija o NCM. [fonte: rejeicoes/ncm]");
        var (servico, _, _) = Criar();

        var r = await servico.ResponderAsync(new PedidoRespostaOri(Escopo, TipoInteracaoAssistente.PerguntaLivre,
            "o cliente 529.982.247-25 (maria@exemplo.com) pode receber?", null, nota.Id), null, default);

        var tudoEnviado = string.Join("\n", _mensagensEnviadas.SelectMany(m => m).Select(m => m.Texto));
        tudoEnviado.Should().NotContain("529.982.247-25").And.NotContain("52998224725")
            .And.NotContain("maria@exemplo.com").And.NotContain("Maria Fictícia").And.NotContain("Rua X");
        tudoEnviado.Should().Contain("\"tipo\":\"PF\"");
        r.DadosPessoaisRemovidos.Should().Be(2);
        r.Texto.Should().Contain("529.982.247-25");
    }

    [Fact]
    public async Task Nota_de_outra_empresa_nao_e_encontrada_pelo_handler_nem_pela_ferramenta()
    {
        var notaAlheia = NotaRejeitada(Guid.NewGuid());
        var (servico, contexto, ferramentas) = Criar();

        var r = await new ExplicarRejeicaoHandler(servico, contexto).Handle(new ExplicarRejeicaoCommand(Escopo, notaAlheia.Id, null), default);
        r.Should().BeNull();

        var consultar = ferramentas.Criar(Escopo, new Dictionary<string, string>(), new List<AcaoUiDto>())
            .Single(f => f.Nome == "consultar_nota");
        var resultado = await consultar.Executar(JsonDocument.Parse($"{{\"nota_id\":\"{notaAlheia.Id}\"}}").RootElement, default);
        resultado.Should().Contain("não encontrada");
    }

    [Fact]
    public async Task Ferramentas_de_preparacao_so_produzem_acoes_para_o_usuario_concluir()
    {
        var nota = NotaRejeitada(_empresaId);
        var acoes = new List<AcaoUiDto>();
        var (_, _, ferramentas) = Criar();
        var lista = ferramentas.Criar(Escopo, new Dictionary<string, string>(), acoes);

        await lista.Single(f => f.Nome == "preparar_nota").Executar(JsonDocument.Parse($"{{\"nota_id\":\"{nota.Id}\",\"ajustes\":\"item1.ncm=69120000\"}}").RootElement, default);
        await lista.Single(f => f.Nome == "abrir_chamado").Executar(JsonDocument.Parse("{\"titulo\":\"Erro ao salvar\",\"severidade\":\"Alta\"}").RootElement, default);

        acoes.Select(a => a.Tipo).Should().Equal("preparar_nota", "confirmar_chamado");
        acoes[0].Parametros["ajustes"].Should().Be("item1.ncm=69120000");
        acoes[1].Parametros["severidade"].Should().Be("Alta");
        _notas.Verify(n => n.UpdateAsync(It.IsAny<NotaFiscal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
