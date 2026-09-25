using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Assistente.Ia;

/// <summary>
/// Ferramentas da Ori (CONTEXTO_AGENTE.md §5). Construídas por requisição: o escopo (empresa, escritório, usuário) fica
/// na closure — o modelo nunca escolhe de qual empresa ler. Resultados projetados e sanitizados. Ferramentas de
/// preparação/registro não concluem nada: produzem <see cref="AcaoUiDto"/> que o usuário confirma na tela.
/// </summary>
public class FerramentasOri
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly INotaFiscalRepository _notas;
    private readonly IEmpresaRepository _empresas;
    private readonly IEscritorioRepository _escritorios;
    private readonly INcmRepository _ncms;
    private readonly ISefazService _sefaz;
    private readonly IBaseConhecimento _base;
    private readonly ISanitizadorIA _sanitizador;
    private readonly MontadorContexto _contexto;
    private readonly IMediator _mediator;
    private readonly ILogger<FerramentasOri> _logger;

    public FerramentasOri(INotaFiscalRepository notas, IEmpresaRepository empresas, IEscritorioRepository escritorios,
        INcmRepository ncms, ISefazService sefaz, IBaseConhecimento baseConhecimento, ISanitizadorIA sanitizador,
        MontadorContexto contexto, IMediator mediator, ILogger<FerramentasOri> logger)
    {
        _notas = notas;
        _empresas = empresas;
        _escritorios = escritorios;
        _ncms = ncms;
        _sefaz = sefaz;
        _base = baseConhecimento;
        _sanitizador = sanitizador;
        _contexto = contexto;
        _mediator = mediator;
        _logger = logger;
    }

    public IReadOnlyList<FerramentaIa> Criar(EscopoOri escopo, IDictionary<string, string> mapa, List<AcaoUiDto> acoes)
    {
        string Saida(object o) => _sanitizador.Sanitizar(JsonSerializer.Serialize(o, Json), mapa).Texto;
        // Argumentos podem conter marcadores ([CHAVE_ACESSO_1]) — reidrata antes de consultar.
        string? Arg(JsonElement a, string nome) =>
            a.ValueKind == JsonValueKind.Object && a.TryGetProperty(nome, out var v) && v.ValueKind != JsonValueKind.Null
                ? _sanitizador.Reidratar(v.ValueKind == JsonValueKind.String ? v.GetString()! : v.GetRawText(), mapa.AsReadOnly())
                : null;
        int? ArgInt(JsonElement a, string nome) => int.TryParse(Arg(a, nome), out var n) ? n : null;

        async Task<NotaFiscal?> Nota(JsonElement a, CancellationToken ct)
        {
            if (Guid.TryParse(Arg(a, "nota_id"), out var id)) return await _contexto.ObterNotaAsync(escopo, id, ct);
            var chave = Arg(a, "chave_acesso");
            if (!string.IsNullOrWhiteSpace(chave))
            {
                var n = await _notas.GetByChaveAcessoAsync(new string(chave.Where(char.IsDigit).ToArray()), ct);
                return n != null && n.EmpresaId == escopo.EmpresaId ? n : null;
            }
            if (ArgInt(a, "numero") is { } numero)
            {
                var empresa = await _empresas.GetByIdAsync(escopo.EmpresaId, ct);
                if (empresa == null) return null;
                var modelo = ArgInt(a, "modelo") == 65 ? TipoNota.NFCe : TipoNota.NFe;
                var serie = ArgInt(a, "serie") ?? (modelo == TipoNota.NFCe ? empresa.SerieNFCe : empresa.SerieNFe);
                return await _notas.GetBySerieNumeroAsync(escopo.EmpresaId, modelo, serie, numero, empresa.AmbienteSefaz, ct);
            }
            return null;
        }

        var lista = new List<FerramentaIa>
        {
            // ---------------------------------------------------------------- leitura
            new("consultar_nota",
                "Consulta uma nota da empresa atual (situação, motivo de rejeição, itens com NCM/CFOP/CST). Informe nota_id, ou chave_acesso, ou numero (+ serie e modelo 55/65).",
                Schema(("nota_id", "string"), ("chave_acesso", "string"), ("numero", "integer"), ("serie", "integer"), ("modelo", "integer")),
                async (a, ct) => await Nota(a, ct) is { } n ? Saida(_contexto.DescreverNota(n, mapa)) : "{\"erro\":\"nota não encontrada nesta empresa\"}"),

            new("listar_rejeicoes_recentes",
                "Lista as rejeições da SEFAZ nas notas da empresa atual nos últimos dias, agrupadas por código.",
                Schema(("dias", "integer")),
                async (a, ct) =>
                {
                    var dias = Math.Clamp(ArgInt(a, "dias") ?? 30, 1, 180);
                    var notas = await _notas.GetByPeriodoAsync(escopo.EmpresaId, DateTime.UtcNow.AddDays(-dias), DateTime.UtcNow, ct);
                    var grupos = notas.Where(n => n.Situacao == SituacaoNota.Rejeitada && n.MotivoRejeicao != null)
                        .GroupBy(n => _base.ExtrairCodigoRejeicao(n.MotivoRejeicao) ?? "sem_codigo")
                        .Select(g => new { codigo = g.Key, quantidade = g.Count(), motivo = g.First().MotivoRejeicao, ultima_nota_id = g.OrderByDescending(n => n.DataEmissao).First().Id })
                        .OrderByDescending(g => g.quantidade).Take(15).ToList();
                    return Saida(new { dias, total_rejeitadas = grupos.Sum(g => g.quantidade), grupos });
                }),

            new("consultar_empresa",
                "Dados fiscais da empresa atual: UF, regime tributário, ambiente SEFAZ e se a inscrição estadual está preenchida. Não traz CNPJ nem razão social.",
                Schema(),
                async (_, ct) => await _empresas.GetByIdAsync(escopo.EmpresaId, ct) is { } e
                    ? Saida(new { e.Uf, regime = e.RegimeTributario.ToString(), ambiente = e.AmbienteSefaz.ToString(), ie_presente = !string.IsNullOrWhiteSpace(e.InscricaoEstadual), e.Cnae, serie_nfe = e.SerieNFe, serie_nfce = e.SerieNFCe, csc_configurado = !string.IsNullOrWhiteSpace(e.CscId) })
                    : "{\"erro\":\"empresa não encontrada\"}"),

            new("consultar_certificado",
                "Situação do certificado digital A1 da empresa atual: se existe, validade e dias para vencer.",
                Schema(),
                async (_, ct) => await _empresas.GetByIdAsync(escopo.EmpresaId, ct) is { } e
                    ? Saida(new { presente = e.CertificadoBytes != null, valido = e.CertificadoValido(), validade = e.CertificadoValidade?.ToString("yyyy-MM-dd"), dias_para_vencer = e.CertificadoValidade is { } v ? (int?)(v - DateTime.UtcNow).TotalDays : null })
                    : "{\"erro\":\"empresa não encontrada\"}"),

            new("consultar_status_sefaz",
                "Verifica agora se o autorizador da SEFAZ da UF da empresa está respondendo.",
                Schema(),
                async (_, ct) =>
                {
                    var e = await _empresas.GetByIdAsync(escopo.EmpresaId, ct);
                    if (e == null) return "{\"erro\":\"empresa não encontrada\"}";
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(15));
                    try { return Saida(new { uf = e.Uf, ambiente = e.AmbienteSefaz.ToString(), em_operacao = await _sefaz.ConsultarStatusServicoAsync(e, cts.Token) }); }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return "{\"em_operacao\":false,\"observacao\":\"sem resposta em 15 segundos\"}"; }
                }),

            new("buscar_ncm",
                "Busca NCM na tabela oficial por código ou descrição; informa se o NCM exige CEST.",
                Schema(("termo", "string")),
                async (a, ct) =>
                {
                    var termo = Arg(a, "termo");
                    if (string.IsNullOrWhiteSpace(termo)) return "{\"erro\":\"informe o termo\"}";
                    var r = await _ncms.BuscarAsync(termo, 8, ct);
                    return Saida(r.Select(n => new { n.Codigo, n.Descricao, exige_cest = n.ExigeCest }));
                }),

            new("consultar_assinatura",
                "Plano e situação da assinatura do escritório (trial, pago, expirado) e dias restantes do teste.",
                Schema(),
                async (_, ct) => await _escritorios.GetByIdAsync(escopo.EscritorioId, ct) is { } esc
                    ? Saida(new { plano = esc.Plano.ToString(), status = esc.CalcularStatusAssinatura().ToString(), dias_trial = esc.DiasRestantesTrial() })
                    : "{\"erro\":\"escritório não encontrado\"}"),

            new("consultar_padroes",
                "Padrões de preenchimento (CFOP, CST/CSOSN) usados nas últimas notas autorizadas da empresa, por destinatário e/ou código de produto.",
                Schema(("destinatario", "string"), ("produto_codigo", "string")),
                async (a, ct) =>
                {
                    try { return Saida(await _mediator.Send(new GetPadroesPreenchimentoQuery(escopo.EmpresaId, Arg(a, "destinatario"), Arg(a, "produto_codigo")), ct)); }
                    catch (InvalidOperationException) { return "{\"erro\":\"consulta de padrões indisponível\"}"; }
                }),

            new("buscar_artigo",
                "Busca artigos adicionais na base de conhecimento por termo ou código de rejeição. Use quando os artigos fornecidos não bastarem.",
                Schema(("termo", "string"), ("codigo_rejeicao", "string")),
                (a, _) =>
                {
                    var codigo = Arg(a, "codigo_rejeicao");
                    var artigos = _base.Rotear(new CriterioRoteamentoKb(codigo == null ? null : new[] { codigo }, Termo: Arg(a, "termo")), 2);
                    return Task.FromResult(artigos.Count == 0 ? "{\"artigos\":[]}" : MontadorPrompt.BlocoArtigos(artigos));
                }),

            // ---------------------------------------------------------------- preparação (usuário conclui)
            new("preparar_nota",
                "Prepara o formulário de emissão a partir de uma nota existente, opcionalmente com ajustes. NÃO emite: o usuário revisa e clica em Emitir.",
                Schema(("nota_id", "string"), ("ajustes", "string"), ("resumo", "string")),
                async (a, ct) =>
                {
                    if (await Nota(a, ct) is not { } n) return "{\"erro\":\"nota não encontrada nesta empresa\"}";
                    var ajustes = Arg(a, "ajustes") ?? "";
                    acoes.Add(new AcaoUiDto("preparar_nota", Arg(a, "resumo") ?? $"Abrir o formulário preenchido a partir da nota {n.Numero}",
                        new() { ["origemNotaId"] = n.Id.ToString(), ["ajustes"] = ajustes }));
                    return "{\"ok\":true,\"observacao\":\"o usuário verá um botão para abrir o formulário preenchido e conferir antes de emitir\"}";
                }),

            new("preparar_cadastro",
                "Prepara o cadastro de destinatário (tipo=Cliente) ou de produtos (tipo=Produto) a partir de uma nota. NÃO salva: o usuário confere e clica em Salvar.",
                Schema(("tipo", "string"), ("nota_id", "string"), ("itens", "string")),
                async (a, ct) =>
                {
                    if (await Nota(a, ct) is not { } n) return "{\"erro\":\"nota não encontrada nesta empresa\"}";
                    var tipo = string.Equals(Arg(a, "tipo"), "Produto", StringComparison.OrdinalIgnoreCase) ? "Produto" : "Cliente";
                    acoes.Add(new AcaoUiDto("preparar_cadastro", tipo == "Cliente" ? "Guardar o destinatário desta nota nos cadastros" : "Guardar os produtos desta nota nos cadastros",
                        new() { ["tipo"] = tipo, ["origemNotaId"] = n.Id.ToString(), ["itens"] = Arg(a, "itens") ?? "" }));
                    return "{\"ok\":true}";
                }),

            new("propor_lembrete",
                "Propõe um lembrete de nota recorrente a partir de uma nota modelo. periodicidade: Semanal, Quinzenal ou Mensal; dia: 1–28 (mensal) ou 0–6 (dia da semana). Só é criado se o usuário confirmar.",
                Schema(("nota_id", "string"), ("periodicidade", "string"), ("dia", "integer")),
                async (a, ct) =>
                {
                    if (await Nota(a, ct) is not { } n) return "{\"erro\":\"nota não encontrada nesta empresa\"}";
                    var periodicidade = Enum.TryParse<PeriodicidadeLembrete>(Arg(a, "periodicidade"), true, out var p) ? p : PeriodicidadeLembrete.Mensal;
                    var dia = ArgInt(a, "dia") ?? Math.Min(n.DataEmissao.Day, 28);
                    acoes.Add(new AcaoUiDto("propor_lembrete", $"Lembrete {periodicidade.ToString().ToLowerInvariant()} para emitir de novo a nota {n.Numero}",
                        new() { ["origemNotaId"] = n.Id.ToString(), ["periodicidade"] = periodicidade.ToString(), ["dia"] = dia.ToString() }));
                    return "{\"ok\":true}";
                }),

            new("sugerir_personalizacao",
                "Sugere ajustar a interface (ex.: ocultar NFC-e) com o motivo. Só muda se o usuário aplicar.",
                Schema(("flag", "string"), ("valor", "string"), ("motivo", "string")),
                (a, _) =>
                {
                    acoes.Add(new AcaoUiDto("sugerir_personalizacao", Arg(a, "motivo") ?? "Ajustar a personalização",
                        new() { ["flag"] = Arg(a, "flag") ?? "", ["valor"] = Arg(a, "valor") ?? "", ["rota"] = "/configuracao-inicial" }));
                    return Task.FromResult("{\"ok\":true}");
                }),

            // ---------------------------------------------------------------- registro (com confirmação)
            new("abrir_chamado",
                "Propõe abrir um chamado de defeito do sistema. O usuário vê o resumo e confirma o envio. severidade: Baixa, Media, Alta ou Critica.",
                Schema(("titulo", "string"), ("descricao", "string"), ("passos", "string"), ("severidade", "string"), ("nota_id", "string")),
                (a, _) =>
                {
                    acoes.Add(new AcaoUiDto("confirmar_chamado", Arg(a, "titulo") ?? "Registrar um problema", new()
                    {
                        ["titulo"] = Arg(a, "titulo") ?? "Problema relatado à Ori",
                        ["descricao"] = Arg(a, "descricao") ?? "",
                        ["passos"] = Arg(a, "passos") ?? "",
                        ["severidade"] = Enum.TryParse<SeveridadeChamado>(Arg(a, "severidade"), true, out var s) ? s.ToString() : nameof(SeveridadeChamado.Media),
                        ["notaId"] = Arg(a, "nota_id") ?? ""
                    }));
                    return Task.FromResult("{\"ok\":true,\"observacao\":\"o usuário vai revisar e confirmar o envio\"}");
                }),

            new("registrar_sinal",
                "Registra um sinal de produto: LacunaConhecimento (pergunta sem cobertura na base — registrada direto), Dor, PedidoFuncionalidade, FriccaoUX ou Elogio (o usuário confirma).",
                Schema(("tipo", "string"), ("resumo", "string"), ("tema", "string")),
                async (a, ct) =>
                {
                    var tipo = Enum.TryParse<TipoSinalProduto>(Arg(a, "tipo"), true, out var t) ? t : TipoSinalProduto.LacunaConhecimento;
                    var resumo = _sanitizador.Sanitizar(Arg(a, "resumo") ?? "", mapa).Texto;
                    if (string.IsNullOrWhiteSpace(resumo)) return "{\"erro\":\"informe o resumo\"}";
                    if (tipo == TipoSinalProduto.LacunaConhecimento)
                    {
                        try { await _mediator.Send(new RegistrarSinalCommand(escopo.EscritorioId, escopo.EmpresaId, escopo.UsuarioId, tipo, resumo, null, Arg(a, "tema")), ct); }
                        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "Falha ao registrar lacuna de conhecimento."); }
                        return "{\"ok\":true}";
                    }
                    acoes.Add(new AcaoUiDto("confirmar_sinal", resumo, new() { ["tipo"] = tipo.ToString(), ["resumo"] = resumo }));
                    return "{\"ok\":true}";
                }),
        };
        return lista;
    }

    private static string Schema(params (string Nome, string Tipo)[] propriedades)
    {
        var props = propriedades.ToDictionary(p => p.Nome, p => new { type = p.Tipo });
        return JsonSerializer.Serialize(new { type = "object", properties = props, additionalProperties = false });
    }
}
