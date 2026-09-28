using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Assistente;
using NfeSaas.Infrastructure.Assistente;

namespace NfeSaas.Tests.Unit.Assistente.P3;

public class AssistenteIAConfigTests
{
    private static AssistenteIA Ia(Action<IaOptions> configurar)
    {
        var opcoes = new AssistenteOptions();
        configurar(opcoes.Ia);
        var monitor = new Moq.Mock<IOptionsMonitor<AssistenteOptions>>();
        monitor.Setup(m => m.CurrentValue).Returns(opcoes);
        return new AssistenteIA(monitor.Object, NullLogger<AssistenteIA>.Instance);
    }

    [Fact]
    public void Sem_chave_a_ia_fica_desabilitada()
    {
        var ia = Ia(_ => { });
        ia.EstaHabilitado(RotaIa.Conversa).Should().BeFalse();
        ia.Modelo(RotaIa.Conversa).Should().Be("desabilitado");
    }

    [Fact]
    public void So_a_chave_basta_endpoint_em_branco_vira_api_da_deepseek()
    {
        var ia = Ia(c => { c.Conversa.ApiKey = "k"; c.Conversa.Endpoint = ""; });
        ia.EstaHabilitado(RotaIa.Conversa).Should().BeTrue();
        ia.Modelo(RotaIa.Conversa).Should().Be("deepseek-flash");
        new EndpointIaOptions { Endpoint = " " }.EndpointEfetivo.Should().Be(EndpointIaOptions.EndpointDeepSeek);
    }

    [Fact]
    public void Fontes_publicas_sem_chave_propria_usa_a_da_conversa()
    {
        Ia(c => c.Conversa.ApiKey = "k").EstaHabilitado(RotaIa.FontesPublicas).Should().BeTrue();
        Ia(c => { c.Conversa.ApiKey = "k"; c.FontesPublicas.ApiKey = "f"; c.FontesPublicas.Modelo = "outro"; })
            .Modelo(RotaIa.FontesPublicas).Should().Be("outro");
    }
}
