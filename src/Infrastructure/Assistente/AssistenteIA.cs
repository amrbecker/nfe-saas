using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NfeSaas.Application.Assistente;
using OpenAI;

namespace NfeSaas.Infrastructure.Assistente;

/// <summary>
/// Cliente de IA da Ori sobre endpoints compatíveis com OpenAI (DeepSeek no Microsoft Foundry para a rota Conversa;
/// API da DeepSeek para FontesPublicas). Loop de ferramentas manual: o servidor executa cada ferramenta com o escopo
/// já capturado (EmpresaId do token), nunca com dados vindos dos argumentos do modelo.
/// </summary>
public class AssistenteIA : IAssistenteIA
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly IOptionsMonitor<AssistenteOptions> _opcoes;
    private readonly ILogger<AssistenteIA> _logger;
    private readonly ConcurrentDictionary<string, IChatClient> _clientes = new();

    public AssistenteIA(IOptionsMonitor<AssistenteOptions> opcoes, ILogger<AssistenteIA> logger)
    {
        _opcoes = opcoes;
        _logger = logger;
    }

    private EndpointIaOptions Config(RotaIa rota) =>
        rota == RotaIa.Conversa ? _opcoes.CurrentValue.Ia.Conversa : _opcoes.CurrentValue.Ia.FontesPublicas;

    public bool EstaHabilitado(RotaIa rota)
    {
        var c = Config(rota);
        return !string.IsNullOrWhiteSpace(c.Endpoint) && !string.IsNullOrWhiteSpace(c.ApiKey) && !string.IsNullOrWhiteSpace(c.Modelo);
    }

    public string Modelo(RotaIa rota) => EstaHabilitado(rota) ? Config(rota).Modelo : "desabilitado";

    public decimal CustoEstimadoUsd(RotaIa rota, UsoIa uso)
    {
        var c = Config(rota);
        var semCache = Math.Max(0, uso.TokensEntrada - uso.TokensCache);
        return (semCache * c.PrecoEntradaSemCache + uso.TokensCache * c.PrecoEntradaComCache + uso.TokensSaida * c.PrecoSaida) / 1_000_000m;
    }

    public async Task<RespostaIa> CompletarAsync(RotaIa rota, IReadOnlyList<MensagemIa> mensagens, OpcoesIa opcoes, CancellationToken ct = default)
    {
        if (!EstaHabilitado(rota)) throw new InvalidOperationException($"IA da rota {rota} não configurada.");
        var cliente = Cliente(rota);

        var historico = mensagens.Select(m => new ChatMessage(m.Papel switch
        {
            PapelIa.Sistema => ChatRole.System,
            PapelIa.Assistente => ChatRole.Assistant,
            _ => ChatRole.User
        }, m.Texto)).ToList();

        var ferramentas = opcoes.Ferramentas ?? Array.Empty<FerramentaIa>();
        var declaracoes = ferramentas
            .Select(f => (AITool)AIFunctionFactory.CreateDeclaration(f.Nome, f.Descricao, JsonDocument.Parse(f.JsonSchemaParametros).RootElement))
            .ToList();

        var uso = UsoIa.Zero;
        var chamadas = new List<ChamadaFerramentaIa>();
        var modelo = Config(rota).Modelo;

        for (var iteracao = 0; ; iteracao++)
        {
            var usarFerramentas = declaracoes.Count > 0 && iteracao < opcoes.MaxIteracoesFerramentas;
            var chatOptions = new ChatOptions
            {
                ModelId = modelo,
                MaxOutputTokens = opcoes.MaxTokens,
                Tools = usarFerramentas ? declaracoes : null,
                ToolMode = usarFerramentas ? ChatToolMode.Auto : null
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(Timeout);
            var resposta = await cliente.GetResponseAsync(historico, chatOptions, cts.Token);
            uso = uso.Somar(ConverterUso(resposta.Usage));
            if (!string.IsNullOrWhiteSpace(resposta.ModelId)) modelo = resposta.ModelId;
            historico.AddRange(resposta.Messages);

            var pedidos = resposta.Messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>().ToList();
            if (pedidos.Count == 0 || !usarFerramentas)
                return new RespostaIa(resposta.Text ?? "", uso, modelo, chamadas);

            var resultados = new List<AIContent>();
            foreach (var pedido in pedidos)
            {
                var argumentos = JsonSerializer.Serialize(pedido.Arguments ?? new Dictionary<string, object?>());
                string resultado;
                var ferramenta = ferramentas.FirstOrDefault(f => f.Nome == pedido.Name);
                if (ferramenta == null)
                {
                    resultado = $"{{\"erro\":\"ferramenta '{pedido.Name}' não existe\"}}";
                }
                else
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(argumentos);
                        resultado = await ferramenta.Executar(doc.RootElement.Clone(), ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Ferramenta {Ferramenta} falhou.", pedido.Name);
                        resultado = "{\"erro\":\"não foi possível executar a consulta agora\"}";
                    }
                }
                var chamada = new ChamadaFerramentaIa(pedido.Name, argumentos, resultado);
                chamadas.Add(chamada);
                opcoes.Progresso?.Report(chamada);
                resultados.Add(new FunctionResultContent(pedido.CallId, resultado));
            }
            historico.Add(new ChatMessage(ChatRole.Tool, resultados));
        }
    }

    private IChatClient Cliente(RotaIa rota)
    {
        var c = Config(rota);
        var chave = $"{rota}|{c.Endpoint}|{c.Modelo}|{c.ApiKey?.GetHashCode()}";
        return _clientes.GetOrAdd(chave, _ =>
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(c.Endpoint!),
                NetworkTimeout = Timeout,
                // 3 tentativas com backoff em 429/5xx/falha de rede (padrão do System.ClientModel).
                RetryPolicy = new ClientRetryPolicy(maxRetries: 2)
            };
            return new OpenAIClient(new ApiKeyCredential(c.ApiKey!), options)
                .GetChatClient(c.Modelo)
                .AsIChatClient();
        });
    }

    private static UsoIa ConverterUso(UsageDetails? u)
    {
        if (u == null) return UsoIa.Zero;
        long cache = u.CachedInputTokenCount ?? 0;
        return new UsoIa(u.InputTokenCount ?? 0, cache, u.OutputTokenCount ?? 0);
    }
}
