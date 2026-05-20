using FluentAssertions;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Tests.Unit.Domain;

public class EscritorioTests
{
    private static Escritorio CriarEscritorio(PlanoSaas plano = PlanoSaas.Basico) =>
        Escritorio.Criar("Escritório Teste", "Teste", "12345678000195",
            "teste@escritorio.com", "11999999999", plano);

    [Fact]
    public void Criar_DefineTrialDe30Dias_AContarDeAgora()
    {
        var antes = DateTime.UtcNow;
        var escritorio = CriarEscritorio();
        var depois = DateTime.UtcNow;

        escritorio.TrialInicioEm.Should().BeOnOrAfter(antes).And.BeOnOrBefore(depois);
        var duracao = escritorio.TrialFimEm - escritorio.TrialInicioEm;
        duracao.Should().Be(TimeSpan.FromDays(Escritorio.DiasTrialPadrao));
        escritorio.PlanoAtivoAteEm.Should().BeNull();
        escritorio.UltimoPagamentoEm.Should().BeNull();
    }

    [Fact]
    public void CalcularStatusAssinatura_DentroDoTrial_RetornaTrialAtivo()
    {
        var escritorio = CriarEscritorio();
        escritorio.CalcularStatusAssinatura().Should().Be(StatusAssinaturaEscritorio.TrialAtivo);
    }

    [Fact]
    public void CalcularStatusAssinatura_TrialEncerradoSemPlanoPago_RetornaTrialExpirado()
    {
        var escritorio = CriarEscritorio();
        var futuro = escritorio.TrialFimEm.AddDays(1);

        escritorio.CalcularStatusAssinatura(futuro)
            .Should().Be(StatusAssinaturaEscritorio.TrialExpirado);
    }

    [Fact]
    public void CalcularStatusAssinatura_ComPlanoAtivoNoFuturo_RetornaPago()
    {
        var escritorio = CriarEscritorio();
        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(30));

        escritorio.CalcularStatusAssinatura().Should().Be(StatusAssinaturaEscritorio.Pago);
    }

    [Fact]
    public void CalcularStatusAssinatura_PlanoPagoPrevaleceMesmoComTrialAtivo()
    {
        // Cliente pagou antes do trial acabar — deve aparecer como Pago, não TrialAtivo,
        // refletindo que ele já saiu do período de avaliação.
        var escritorio = CriarEscritorio();
        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(60));

        escritorio.CalcularStatusAssinatura().Should().Be(StatusAssinaturaEscritorio.Pago);
    }

    [Fact]
    public void CalcularStatusAssinatura_PlanoPagoVencido_VoltaParaTrialExpirado()
    {
        var escritorio = CriarEscritorio();
        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(60));
        var referenciaPosTrialEPosPlano = escritorio.TrialFimEm.AddDays(1).AddDays(60);

        escritorio.CalcularStatusAssinatura(referenciaPosTrialEPosPlano)
            .Should().Be(StatusAssinaturaEscritorio.TrialExpirado);
    }

    [Fact]
    public void CalcularStatusAssinatura_Suspenso_DominaTodosOsDemaisEstados()
    {
        var escritorio = CriarEscritorio();
        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(60));
        escritorio.Suspender();

        escritorio.CalcularStatusAssinatura().Should().Be(StatusAssinaturaEscritorio.Suspenso);
    }

    [Fact]
    public void PodeAcessar_TrialAtivo_RetornaTrue()
    {
        var escritorio = CriarEscritorio();
        escritorio.PodeAcessar().Should().BeTrue();
    }

    [Fact]
    public void PodeAcessar_Pago_RetornaTrue()
    {
        var escritorio = CriarEscritorio();
        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(30));
        escritorio.PodeAcessar().Should().BeTrue();
    }

    [Fact]
    public void PodeAcessar_TrialExpirado_RetornaFalse()
    {
        var escritorio = CriarEscritorio();
        escritorio.PodeAcessar(escritorio.TrialFimEm.AddDays(1)).Should().BeFalse();
    }

    [Fact]
    public void PodeAcessar_Suspenso_RetornaFalse()
    {
        var escritorio = CriarEscritorio();
        escritorio.Suspender();
        escritorio.PodeAcessar().Should().BeFalse();
    }

    [Fact]
    public void DiasRestantesTrial_NoInicio_Retorna30()
    {
        var escritorio = CriarEscritorio();
        // Math.Ceiling pode dar 30 ou 31 dependendo de microssegundos passados desde Criar.
        escritorio.DiasRestantesTrial().Should().BeInRange(29, 30);
    }

    [Fact]
    public void DiasRestantesTrial_AposExpirar_RetornaZero()
    {
        var escritorio = CriarEscritorio();
        escritorio.DiasRestantesTrial(escritorio.TrialFimEm.AddDays(1)).Should().Be(0);
    }

    [Fact]
    public void AtivarPlanoPago_DataNoPassado_LancaInvalidOperationException()
    {
        var escritorio = CriarEscritorio();
        var act = () => escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(-1));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AtivarPlanoPago_DataFutura_DefinePlanoEUltimoPagamento()
    {
        var escritorio = CriarEscritorio();
        var ativoAte = DateTime.UtcNow.AddDays(365);
        var antesPagamento = DateTime.UtcNow;

        escritorio.AtivarPlanoPago(ativoAte);

        escritorio.PlanoAtivoAteEm.Should().Be(ativoAte);
        escritorio.UltimoPagamentoEm.Should().NotBeNull();
        escritorio.UltimoPagamentoEm!.Value.Should().BeOnOrAfter(antesPagamento);
    }

    [Fact]
    public void AtivarPlanoPago_ComMomentoPagamentoExplicito_UsaOValorPassado()
    {
        var escritorio = CriarEscritorio();
        var momento = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        escritorio.AtivarPlanoPago(DateTime.UtcNow.AddDays(30), momento);

        escritorio.UltimoPagamentoEm.Should().Be(momento);
    }

    [Fact]
    public void Suspender_DesativaEscritorio()
    {
        var escritorio = CriarEscritorio();
        escritorio.Suspender();
        escritorio.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Reativar_AposSuspender_RestabeleceAtivo()
    {
        var escritorio = CriarEscritorio();
        escritorio.Suspender();
        escritorio.Reativar();
        escritorio.Ativo.Should().BeTrue();
    }

    [Fact]
    public void AtualizarPlano_TrocaPlano()
    {
        var escritorio = CriarEscritorio(PlanoSaas.Basico);
        escritorio.AtualizarPlano(PlanoSaas.Enterprise);
        escritorio.Plano.Should().Be(PlanoSaas.Enterprise);
    }
}
