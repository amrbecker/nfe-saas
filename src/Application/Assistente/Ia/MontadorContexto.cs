using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Assistente.Ia;

/// <summary>Escopo da requisição — sempre vindo do token, nunca do body nem do modelo.</summary>
public record EscopoOri(Guid UsuarioId, Guid EscritorioId, Guid EmpresaId, string Papel);

/// <summary>
/// Monta o bloco &lt;contexto&gt; enviado ao modelo: dados do servidor (empresa, assinatura, data) + contexto da tela
/// informado pela WebUI + a nota em foco. Tudo sanitizado e sem documento/nome/endereço (só atributos).
/// </summary>
public class MontadorContexto
{
    private readonly IEmpresaRepository _empresas;
    private readonly IEscritorioRepository _escritorios;
    private readonly IConfiguracaoEmpresaRepository _configuracoes;
    private readonly INotaFiscalRepository _notas;
    private readonly ISanitizadorIA _sanitizador;
    private readonly IBaseConhecimento _base;

    public MontadorContexto(IEmpresaRepository empresas, IEscritorioRepository escritorios,
        IConfiguracaoEmpresaRepository configuracoes, INotaFiscalRepository notas, ISanitizadorIA sanitizador,
        IBaseConhecimento baseConhecimento)
    {
        _empresas = empresas;
        _escritorios = escritorios;
        _configuracoes = configuracoes;
        _notas = notas;
        _sanitizador = sanitizador;
        _base = baseConhecimento;
    }

    /// <summary>Nota da empresa do escopo, ou null (inexistente ou de outra empresa).</summary>
    public async Task<NotaFiscal?> ObterNotaAsync(EscopoOri escopo, Guid? notaId, CancellationToken ct)
    {
        if (notaId is not { } id || id == Guid.Empty) return null;
        var nota = await _notas.GetByIdAsync(id, ct);
        return nota != null && nota.EmpresaId == escopo.EmpresaId ? nota : null;
    }

    public async Task<Dictionary<string, object?>> MontarAsync(EscopoOri escopo, ContextoTelaDto? tela, NotaFiscal? nota,
        IDictionary<string, string> mapa, CancellationToken ct)
    {
        var empresa = await _empresas.GetByIdAsync(escopo.EmpresaId, ct);
        var escritorio = await _escritorios.GetByIdAsync(escopo.EscritorioId, ct);
        var config = await _configuracoes.GetByEmpresaAsync(escopo.EmpresaId, ct);
        var agora = DateTime.UtcNow;

        var contexto = new Dictionary<string, object?>
        {
            ["data_hoje"] = agora.ToString("yyyy-MM-dd"),
            ["papel_usuario"] = escopo.Papel,
            ["empresa"] = empresa == null ? null : new Dictionary<string, object?>
            {
                ["uf"] = empresa.Uf,
                ["regime"] = empresa.RegimeTributario.ToString(),
                ["ambiente"] = empresa.AmbienteSefaz.ToString(),
                ["perfil"] = config?.PerfilCliente.ToString(),
                ["opera_icms_st"] = config?.OperaIcmsSt,
                ["emite_consumidor_final"] = config?.EmiteParaConsumidorFinal,
                ["certificado_valido"] = empresa.CertificadoValido(),
                ["certificado_dias_para_vencer"] = empresa.CertificadoValidade is { } v ? (int)(v - agora).TotalDays : null
            },
            ["assinatura"] = escritorio == null ? null : new Dictionary<string, object?>
            {
                ["plano"] = escritorio.Plano.ToString(),
                ["status"] = escritorio.CalcularStatusAssinatura(agora).ToString(),
                ["dias_trial"] = escritorio.CalcularStatusAssinatura(agora) == StatusAssinaturaEscritorio.TrialAtivo
                    ? escritorio.DiasRestantesTrial(agora) : null
            }
        };

        if (tela != null) contexto["tela"] = SanitizarTela(tela, mapa);
        if (nota != null) contexto["nota"] = DescreverNota(nota, mapa);
        return contexto;
    }

    public object SanitizarTela(ContextoTelaDto t, IDictionary<string, string> mapa)
    {
        string? S(string? s) => s == null ? null : _sanitizador.Sanitizar(s, mapa).Texto;
        return new Dictionary<string, object?>
        {
            ["tela"] = t.Tela,
            ["operacao"] = t.Op,
            ["foco"] = t.Foco == null ? null : new
            {
                campo = t.Foco.Campo,
                rotulo = t.Foco.Rotulo,
                valor = CamposOri.EhSensivel(t.Foco.Campo) ? null : S(t.Foco.Valor),
                item = t.Foco.Item
            },
            ["destinatario"] = t.Dest,
            ["erros_campos"] = t.Erros?.Select(e => new { e.Campo, mensagem = S(e.Mensagem), e.Item }).ToList(),
            ["erros_api"] = t.Api?.Select(e => new { e.Rota, e.Status, e.Codigo, mensagem = S(e.Mensagem), ha_segundos = e.HaSegundos }).ToList(),
            ["rastro"] = t.Rastro?.TakeLast(8).Select(r => S(r)).ToList()
        };
    }

    public object DescreverNota(NotaFiscal n, IDictionary<string, string> mapa)
    {
        var codigo = _base.ExtrairCodigoRejeicao(n.MotivoRejeicao);
        var rejeicaoDeValores = n.MotivoRejeicao != null &&
            (n.MotivoRejeicao.Contains("total", StringComparison.OrdinalIgnoreCase)
             || n.MotivoRejeicao.Contains("valor", StringComparison.OrdinalIgnoreCase)
             || n.MotivoRejeicao.Contains("soma", StringComparison.OrdinalIgnoreCase));
        var dest = _sanitizador.DescreverPessoa(n.DestinatarioCpfCnpj, n.DestinatarioUf, n.DestinatarioInscricaoEstadual);

        return new Dictionary<string, object?>
        {
            ["id"] = n.Id,
            ["modelo"] = (int)n.Tipo,
            ["serie"] = n.Serie,
            ["numero"] = n.Numero,
            ["situacao"] = n.Situacao.ToString(),
            ["ambiente"] = n.Ambiente.ToString(),
            ["data_emissao"] = n.DataEmissao.ToString("yyyy-MM-dd"),
            ["motivo_rejeicao"] = n.MotivoRejeicao == null ? null : _sanitizador.Sanitizar(n.MotivoRejeicao, mapa).Texto,
            ["codigo_rejeicao"] = codigo,
            ["destinatario"] = new { dest.Tipo, doc_valido = dest.DocValido, dest.Uf, ie_presente = dest.IePresente, ie_formato_valido_uf = dest.IeFormatoValidoUf },
            ["itens"] = n.Itens.OrderBy(i => i.NumeroItem).Take(30).Select(i => new Dictionary<string, object?>
            {
                ["item"] = i.NumeroItem,
                ["codigo_produto"] = _sanitizador.Sanitizar(i.CodigoProduto, mapa).Texto,
                ["descricao"] = _sanitizador.Sanitizar(i.Descricao, mapa).Texto,
                ["ncm"] = i.Ncm,
                ["cest"] = i.Cest,
                ["cfop"] = i.Cfop,
                ["cst_icms"] = ((int)i.CstIcms).ToString("00"),
                ["csosn"] = i.CsosnIcms is { } cs ? ((int)cs).ToString() : null,
                ["cst_ibs_cbs"] = i.CstIbsCbs,
                ["cclasstrib"] = i.ClassTribIbsCbs,
                ["unidade"] = i.UnidadeComercial,
                ["valor_total"] = rejeicaoDeValores ? i.ValorTotal : null
            }).ToList(),
            ["totais"] = rejeicaoDeValores
                ? new { produtos = n.TotalProdutos, desconto = n.TotalDesconto, frete = n.TotalFrete, nota = n.TotalNota, icms = n.TotalIcms }
                : null
        };
    }
}
