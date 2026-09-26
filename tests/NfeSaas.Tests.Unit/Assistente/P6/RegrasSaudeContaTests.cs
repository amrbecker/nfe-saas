using FluentAssertions;
using NfeSaas.Application.Assistente.Cs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P6;

public class RegrasSaudeContaTests
{
    private static readonly DateTime Agora = new(2026, 9, 10, 15, 0, 0, DateTimeKind.Utc);

    private static EmpresaSaudeAssistente Empresa(DateTime? certificado = null, bool configurada = true, int criadaHaDias = 90,
        AmbienteSefaz ambiente = AmbienteSefaz.Producao) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Cerâmica Aurora", Agora.AddDays(-criadaHaDias), certificado, certificado != null, configurada, ambiente);

    private static NotaResumoAssistente Nota(SituacaoNota s, DateTime quando, string? motivo = null, string doc = "11222333000181", string produto = "P1") =>
        new(Guid.NewGuid(), Guid.Empty, TipoNota.NFe, 10, doc, "SP", s, AmbienteSefaz.Producao, quando, motivo,
            new[] { new ItemResumoAssistente(1, produto, "Produto", "69120000", null, "5102", 0, 102, "UN") });

    private static List<AlertaProposto> Avaliar(EmpresaSaudeAssistente e, IEnumerable<NotaResumoAssistente>? notas = null, bool teveAutorizada = true,
        bool homolog = false, bool producao = true, DateTime? agora = null) =>
        RegrasSaudeConta.AvaliarEmpresa(new FotoEmpresa(e, (notas ?? Array.Empty<NotaResumoAssistente>()).ToList(), teveAutorizada, homolog, producao),
            agora ?? Agora, m => m?.Length > 5 ? m.Substring(1, 3) : null).ToList();

    [Theory]
    [InlineData(40, null)]
    [InlineData(30, "30")]
    [InlineData(20, "30")]
    [InlineData(10, "15")]
    [InlineData(5, "7")]
    [InlineData(1, "1")]
    [InlineData(-2, "vencido")]
    public void Certificado_gera_alerta_no_marco_certo(int dias, string? marco)
    {
        var alertas = Avaliar(Empresa(Agora.Date.AddDays(dias))).Where(a => a.Tipo == TipoAlertaCs.CertificadoVencendo).ToList();
        if (marco == null) alertas.Should().BeEmpty();
        else alertas.Should().ContainSingle().Which.Chave.Should().EndWith($":{marco}");
    }

    [Fact]
    public void Chave_do_certificado_muda_quando_o_certificado_e_renovado()
    {
        var a = Avaliar(Empresa(Agora.AddDays(5))).Single(x => x.Tipo == TipoAlertaCs.CertificadoVencendo);
        var b = Avaliar(Empresa(Agora.AddDays(370))).Where(x => x.Tipo == TipoAlertaCs.CertificadoVencendo);
        b.Should().BeEmpty();
        a.Link.Should().Be("/certificado");
    }

    [Fact]
    public void Rejeicao_repetida_so_com_tres_ou_mais_na_semana()
    {
        var e = Empresa();
        var duas = Enumerable.Range(1, 2).Select(i => Nota(SituacaoNota.Rejeitada, Agora.AddDays(-i), "[778] NCM"));
        Avaliar(e, duas).Should().NotContain(a => a.Tipo == TipoAlertaCs.RejeicaoRepetida);

        var tres = Enumerable.Range(1, 3).Select(i => Nota(SituacaoNota.Rejeitada, Agora.AddDays(-i), "[778] NCM"));
        Avaliar(e, tres).Should().ContainSingle(a => a.Tipo == TipoAlertaCs.RejeicaoRepetida && a.Mensagem.Contains("778"));
    }

    [Fact]
    public void Onboarding_parado_lista_o_que_falta()
    {
        var alerta = Avaliar(Empresa(configurada: false, criadaHaDias: 5), teveAutorizada: false)
            .Single(a => a.Tipo == TipoAlertaCs.OnboardingParado);
        alerta.Mensagem.Should().Contain("certificado").And.Contain("configuração inicial").And.Contain("primeira nota");
        Avaliar(Empresa(configurada: false, criadaHaDias: 1), teveAutorizada: false).Should().NotContain(a => a.Tipo == TipoAlertaCs.OnboardingParado);
    }

    [Fact]
    public void So_homologacao_depois_de_14_dias()
    {
        Avaliar(Empresa(ambiente: AmbienteSefaz.Homologacao, criadaHaDias: 20), homolog: true, producao: false)
            .Should().Contain(a => a.Tipo == TipoAlertaCs.SoHomologacao);
        Avaliar(Empresa(ambiente: AmbienteSefaz.Homologacao, criadaHaDias: 10), homolog: true, producao: false)
            .Should().NotContain(a => a.Tipo == TipoAlertaCs.SoHomologacao);
    }

    [Fact]
    public void Detecta_nota_recorrente_mensal()
    {
        var mensais = new[] { 90, 60, 30 }.Select(d => Nota(SituacaoNota.Autorizada, Agora.AddDays(-d))).ToList();
        var alerta = Avaliar(Empresa(), mensais).Single(a => a.Tipo == TipoAlertaCs.NotaRecorrente);
        alerta.Link.Should().Be($"/lembretes?notaModelo={mensais[^1].Id}");

        var irregulares = new[] { 90, 80, 30 }.Select(d => Nota(SituacaoNota.Autorizada, Agora.AddDays(-d)));
        Avaliar(Empresa(), irregulares).Should().NotContain(a => a.Tipo == TipoAlertaCs.NotaRecorrente);
    }

    [Fact]
    public void Uso_caindo_e_alerta_interno()
    {
        var antigas = Enumerable.Range(8, 28).Select(d => Nota(SituacaoNota.Autorizada, Agora.AddDays(-d), doc: $"{d}", produto: $"P{d}"));
        var alerta = Avaliar(Empresa(), antigas).Single(a => a.Tipo == TipoAlertaCs.UsoCaindo);
        alerta.Interno.Should().BeTrue();
    }

    [Fact]
    public void Trial_avisa_nos_marcos_7_e_2()
    {
        var esc = Escritorio.Criar("Escritório", "Esc", "99999999000191", "a@a.com", null, PlanoSaas.Basico);
        RegrasSaudeConta.AvaliarEscritorio(esc, DateTime.UtcNow).Should().BeEmpty();
        RegrasSaudeConta.AvaliarEscritorio(esc, DateTime.UtcNow.AddDays(10)).Should().BeEmpty();
        RegrasSaudeConta.AvaliarEscritorio(esc, DateTime.UtcNow.AddDays(24)).Should().ContainSingle(a => a.Chave.EndsWith(":7"));
        RegrasSaudeConta.AvaliarEscritorio(esc, DateTime.UtcNow.AddDays(29)).Should().ContainSingle(a => a.Chave.EndsWith(":2"));
    }

    [Theory]
    [InlineData("{\"cStat\":\"778\",\"cpf\":\"123\",\"email_dest\":\"x\"}", "{\"cStat\":\"778\"}")]
    [InlineData("não é json", null)]
    [InlineData("[1,2]", null)]
    public void Telemetria_descarta_chaves_com_cara_de_dado_pessoal(string entrada, string? saida) =>
        RegistrarEventosHandler.LimparDados(entrada).Should().Be(saida);

    [Fact]
    public void Lembrete_mensal_agenda_proximas_datas()
    {
        var l = LembreteEmissao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Aluguel", PeriodicidadeLembrete.Mensal, 5, Agora);
        l.ProximaEm.Should().Be(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));
        l.RegistrarAviso(new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc));
        l.ProximaEm.Should().Be(new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc));

        var acao = () => LembreteEmissao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", PeriodicidadeLembrete.Mensal, 31, Agora);
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }
}
