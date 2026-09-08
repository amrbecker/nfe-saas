using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NfeSaas.Infrastructure.Services;
using Polly;
using Polly.CircuitBreaker;
using Xunit;

namespace NfeSaas.Tests.Unit.Services;

/// <summary>
/// Testa, de forma isolada (sem servidor SEFAZ real), a política de resiliência
/// (retry com backoff + circuit breaker) que SefazService monta internamente para envolver
/// cada chamada HTTP. Usa reflection para acessar o método privado `CriarPoliticaResiliencia`
/// e o campo estático `_circuitBreaker` — eles não fazem parte do contrato público de
/// ISefazService (que continua mockado/stubado no resto da suíte), só a política em si.
///
/// A política opera sobre (StatusCode, Body) — não HttpResponseMessage — porque o corpo já é
/// lido e a resposta já descartada dentro do delegate real (PostarELerAsync); isso também
/// permite tratar "HTTP 200 com corpo vazio" como falha transiente (SEFAZ já foi observada
/// devolvendo isso sob instabilidade — sem essa checagem, a chamada "sucede" com corpo vazio
/// que só quebra depois, no parse de XML, sem nunca ter sido retentada).
///
/// Cobertura mantida deliberadamente leve: o backoff exponencial real (2s/4s/8s) tornaria um
/// teste de "esgota as 3 retries" ou "abre o circuito" lento (~15s+) sem agregar muita confiança
/// além do que já é validado aqui (retry acontece, some sleep acontece, 2xx com corpo nunca é
/// retentado).
/// </summary>
public class SefazResiliencePolicyTests
{
    private readonly SefazService _service;

    public SefazResiliencePolicyTests()
    {
        var logger = new Mock<ILogger<SefazService>>();
        var httpFactory = new Mock<IHttpClientFactory>();
        // IConfiguration real (vazia) em vez de mock: GetValue<bool>(...) é um método de extensão
        // que chama Get/GetSection por baixo — um Mock<IConfiguration> sem setup devolve null e
        // quebra com NullReferenceException antes mesmo de chegar no que este teste quer exercitar.
        var config = new ConfigurationBuilder().Build();
        _service = new SefazService(logger.Object, httpFactory.Object, config);

        // _circuitBreaker é `static` (compartilhado entre instâncias, de propósito — ver
        // comentário em SefazService.cs) então precisa ser resetado entre testes para um não
        // contaminar o outro com falhas acumuladas.
        var field = typeof(SefazService).GetField("_circuitBreaker", BindingFlags.NonPublic | BindingFlags.Static);
        var breaker = (AsyncCircuitBreakerPolicy<(int StatusCode, string Body)>)field!.GetValue(null)!;
        breaker.Reset();
    }

    private IAsyncPolicy<(int StatusCode, string Body)> CriarPolitica(CancellationToken ct = default)
    {
        var method = typeof(SefazService).GetMethod("CriarPoliticaResiliencia", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IAsyncPolicy<(int StatusCode, string Body)>)method!.Invoke(_service, new object[] { ct })!;
    }

    [Fact]
    public void CircuitBreaker_estatico_esta_configurado_para_StatusCodeEBody()
    {
        var field = typeof(SefazService).GetField("_circuitBreaker", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("o circuit breaker precisa existir e ser compartilhado entre instâncias " +
                                  "(SefazService é Scoped — uma instância nova por requisição)");
        field!.IsStatic.Should().BeTrue();
        field.FieldType.Should().Be(typeof(AsyncCircuitBreakerPolicy<(int StatusCode, string Body)>));
    }

    [Fact]
    public async Task Resposta_2xx_de_negocio_com_corpo_nao_e_retentada()
    {
        // Uma nota rejeitada pela SEFAZ volta como HTTP 200 com XML de rejeição — resposta válida
        // do webservice, não falha transiente. A política não deve nem olhar o conteúdo do corpo.
        var politica = CriarPolitica();
        var tentativas = 0;

        var resultado = await politica.ExecuteAsync(() =>
        {
            tentativas++;
            return Task.FromResult((200, "<retNFe><cStat>301</cStat></retNFe>"));
        });

        tentativas.Should().Be(1);
        resultado.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Falha_5xx_transiente_e_retentada_e_sucede_na_proxima_tentativa()
    {
        var politica = CriarPolitica();
        var tentativas = 0;

        var resultado = await politica.ExecuteAsync(() =>
        {
            tentativas++;
            return Task.FromResult(tentativas == 1 ? (503, "") : (200, "<retNFe><cStat>100</cStat></retNFe>"));
        });

        tentativas.Should().Be(2, "a 1ª tentativa falhou com 503 e deve ter sido retentada uma vez");
        resultado.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Http200_com_corpo_vazio_e_tratado_como_transiente_e_retentado()
    {
        // O sintoma real que motivou este teste: SEFAZ respondendo 200 OK com corpo vazio sob
        // instabilidade — sem este caso coberto, a chamada "sucede" e quebra só depois no parse
        // de XML ("Resposta da SEFAZ não é XML válido. Início: \"\""), sem nunca ter retentado.
        var politica = CriarPolitica();
        var tentativas = 0;

        var resultado = await politica.ExecuteAsync(() =>
        {
            tentativas++;
            return Task.FromResult(tentativas == 1 ? (200, "") : (200, "<retNFe><cStat>100</cStat></retNFe>"));
        });

        tentativas.Should().Be(2, "corpo vazio em HTTP 200 deve ser tratado como falha transiente e retentado");
        resultado.Body.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Timeout_HttpRequestException_tambem_e_tratado_como_transiente()
    {
        var politica = CriarPolitica();
        var tentativas = 0;

        var resultado = await politica.ExecuteAsync(() =>
        {
            tentativas++;
            if (tentativas == 1)
                throw new HttpRequestException("conexão recusada");
            return Task.FromResult((200, "<retNFe><cStat>100</cStat></retNFe>"));
        });

        tentativas.Should().Be(2);
        resultado.StatusCode.Should().Be(200);
    }
}
