using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Services;

public static class CfopValidator
{
    // Simplified CFOP table: code -> (description, permitido para saida, permitido para entrada)
    private static readonly Dictionary<string, CfopInfo> _cfops = new()
    {
        // === SAÍDA (5xxx = intraestadual, 6xxx = interestadual, 7xxx = exportação) ===
        ["5101"] = new("Venda de produção do estabelecimento", Saida: true, Entrada: false, Interestadual: false),
        ["5102"] = new("Venda de mercadoria adquirida ou recebida de terceiros", Saida: true, Entrada: false, Interestadual: false),
        ["5103"] = new("Venda de produção do estabelecimento, efetuada fora do estabelecimento", Saida: true, Entrada: false, Interestadual: false),
        ["5104"] = new("Venda de mercadoria adquirida ou recebida de terceiros, efetuada fora do estabelecimento", Saida: true, Entrada: false, Interestadual: false),
        ["5110"] = new("Venda de produção do estabelecimento, destinada a zona franca de Manaus", Saida: true, Entrada: false, Interestadual: false),
        ["5111"] = new("Venda de mercadoria adquirida ou recebida de terceiros, destinada a zona franca de Manaus", Saida: true, Entrada: false, Interestadual: false),
        ["5201"] = new("Devolução de compra para industrialização", Saida: true, Entrada: false, Interestadual: false),
        ["5202"] = new("Devolução de compra para comercialização", Saida: true, Entrada: false, Interestadual: false),
        ["5403"] = new("Venda de mercadoria sujeita ao regime de substituição tributária", Saida: true, Entrada: false, Interestadual: false),
        ["5405"] = new("Venda de mercadoria adquirida com substituição tributária", Saida: true, Entrada: false, Interestadual: false),
        ["5411"] = new("Devolução de compra para industrialização em zona franca de Manaus", Saida: true, Entrada: false, Interestadual: false),
        ["5551"] = new("Venda de bem do ativo imobilizado", Saida: true, Entrada: false, Interestadual: false),
        ["5556"] = new("Venda de material de uso ou consumo", Saida: true, Entrada: false, Interestadual: false),
        ["5667"] = new("Venda de combustível ou lubrificante adquirido", Saida: true, Entrada: false, Interestadual: false),
        ["5901"] = new("Remessa para industrialização por encomenda", Saida: true, Entrada: false, Interestadual: false),
        ["5910"] = new("Remessa em bonificação", Saida: true, Entrada: false, Interestadual: false),
        ["5911"] = new("Remessa de amostra grátis", Saida: true, Entrada: false, Interestadual: false),
        ["5920"] = new("Remessa de vasilhames e recipientes", Saida: true, Entrada: false, Interestadual: false),
        ["5922"] = new("Lançamento efetuado a título de simples faturamento", Saida: true, Entrada: false, Interestadual: false),
        ["5923"] = new("Remessa de estoque em consignação mercantil", Saida: true, Entrada: false, Interestadual: false),
        ["5924"] = new("Remessa para venda fora do estabelecimento", Saida: true, Entrada: false, Interestadual: false),
        ["5925"] = new("Remessa para exposição ou feira", Saida: true, Entrada: false, Interestadual: false),
        ["5929"] = new("Lançamento efetuado em decorrência de emissão de documento fiscal", Saida: true, Entrada: false, Interestadual: false),
        ["5933"] = new("Prestação de serviço tributada pelo ISSQN", Saida: true, Entrada: false, Interestadual: false),

        // Interestadual saída
        ["6101"] = new("Venda de produção do estabelecimento", Saida: true, Entrada: false, Interestadual: true),
        ["6102"] = new("Venda de mercadoria adquirida ou recebida de terceiros", Saida: true, Entrada: false, Interestadual: true),
        ["6103"] = new("Venda de produção do estabelecimento, efetuada fora do estabelecimento", Saida: true, Entrada: false, Interestadual: true),
        ["6104"] = new("Venda de mercadoria adquirida ou recebida de terceiros, efetuada fora do estabelecimento", Saida: true, Entrada: false, Interestadual: true),
        ["6201"] = new("Devolução de compra para industrialização", Saida: true, Entrada: false, Interestadual: true),
        ["6202"] = new("Devolução de compra para comercialização", Saida: true, Entrada: false, Interestadual: true),
        ["6403"] = new("Venda de mercadoria sujeita ao regime de substituição tributária", Saida: true, Entrada: false, Interestadual: true),
        ["6551"] = new("Venda de bem do ativo imobilizado", Saida: true, Entrada: false, Interestadual: true),
        ["6556"] = new("Venda de material de uso ou consumo", Saida: true, Entrada: false, Interestadual: true),
        ["6901"] = new("Remessa para industrialização por encomenda", Saida: true, Entrada: false, Interestadual: true),
        ["6910"] = new("Remessa em bonificação", Saida: true, Entrada: false, Interestadual: true),
        ["6911"] = new("Remessa de amostra grátis", Saida: true, Entrada: false, Interestadual: true),

        // Exportação
        ["7101"] = new("Venda de produção do estabelecimento", Saida: true, Entrada: false, Interestadual: true),
        ["7102"] = new("Venda de mercadoria adquirida ou recebida de terceiros", Saida: true, Entrada: false, Interestadual: true),

        // === ENTRADA (1xxx = intraestadual, 2xxx = interestadual, 3xxx = importação) ===
        ["1101"] = new("Compra para industrialização", Saida: false, Entrada: true, Interestadual: false),
        ["1102"] = new("Compra para comercialização", Saida: false, Entrada: true, Interestadual: false),
        ["1201"] = new("Devolução de venda de produção do estabelecimento", Saida: false, Entrada: true, Interestadual: false),
        ["1202"] = new("Devolução de venda de mercadoria adquirida", Saida: false, Entrada: true, Interestadual: false),
        ["1403"] = new("Compra de mercadoria sujeita ao regime de substituição tributária", Saida: false, Entrada: true, Interestadual: false),
        ["2101"] = new("Compra para industrialização", Saida: false, Entrada: true, Interestadual: true),
        ["2102"] = new("Compra para comercialização", Saida: false, Entrada: true, Interestadual: true),
        ["2201"] = new("Devolução de venda de produção do estabelecimento", Saida: false, Entrada: true, Interestadual: true),
        ["2202"] = new("Devolução de venda de mercadoria adquirida", Saida: false, Entrada: true, Interestadual: true),
        ["3101"] = new("Compra de produção do estabelecimento do exterior", Saida: false, Entrada: true, Interestadual: true),
        ["3102"] = new("Compra de mercadoria do exterior", Saida: false, Entrada: true, Interestadual: true),
    };

    public static bool Existe(string? cfop)
    {
        if (string.IsNullOrWhiteSpace(cfop)) return false;
        return _cfops.ContainsKey(cfop.Trim());
    }

    public static bool ValidarParaSaida(string? cfop, bool interestadual)
    {
        if (string.IsNullOrWhiteSpace(cfop)) return false;
        if (!_cfops.TryGetValue(cfop.Trim(), out var info)) return false;
        if (!info.Saida) return false;
        return interestadual == info.Interestadual;
    }

    public static bool ValidarParaEntrada(string? cfop, bool interestadual)
    {
        if (string.IsNullOrWhiteSpace(cfop)) return false;
        if (!_cfops.TryGetValue(cfop.Trim(), out var info)) return false;
        if (!info.Entrada) return false;
        return interestadual == info.Interestadual;
    }

    public static string? ObterDescricao(string? cfop)
    {
        if (string.IsNullOrWhiteSpace(cfop)) return null;
        return _cfops.TryGetValue(cfop.Trim(), out var info) ? info.Descricao : null;
    }

    // Returns true for CFOP prefixed 5/6/7 (saída), false for 1/2/3 (entrada)
    public static bool EhSaida(string? cfop)
    {
        if (string.IsNullOrWhiteSpace(cfop) || cfop.Length < 1) return false;
        return cfop[0] is '5' or '6' or '7';
    }

    public static bool EhInterestadual(string? cfop)
    {
        if (string.IsNullOrWhiteSpace(cfop) || cfop.Length < 1) return false;
        return cfop[0] is '2' or '6' or '3' or '7';
    }

    /// <summary>
    /// Lista todos os CFOPs do catálogo com seus metadados.
    /// </summary>
    public static IReadOnlyDictionary<string, CfopInfo> ListarTodos() => _cfops;

    /// <summary>
    /// Lista CFOPs filtrados por sentido (saída/entrada) e abrangência (intra/interestadual).
    /// Por padrão exclui CFOPs de exterior (3xxx/7xxx) — passe <paramref name="exterior"/>=true para incluí-los.
    /// </summary>
    public static IEnumerable<CfopOpcao> Listar(bool saida, bool interestadual, bool exterior = false) =>
        _cfops
            .Where(kv => kv.Value.Saida == saida && kv.Value.Entrada == !saida && kv.Value.Interestadual == interestadual)
            .Where(kv => exterior
                ? kv.Key.StartsWith(saida ? "7" : "3")
                : !kv.Key.StartsWith("7") && !kv.Key.StartsWith("3"))
            .Select(kv => new CfopOpcao(kv.Key, kv.Value.Descricao, kv.Value.Interestadual, kv.Value.Saida))
            .OrderBy(c => c.Codigo);

    /// <summary>
    /// Sugere CFOPs ordenados por relevância dado o contexto fiscal da operação.
    /// O primeiro item da lista é a sugestão padrão (~80% dos casos comuns).
    /// </summary>
    /// <param name="ufEmitente">UF do emissor (ex.: "SP")</param>
    /// <param name="ufDestino">UF do destinatário</param>
    /// <param name="operacao">Saída (venda) ou Entrada (compra/recebimento)</param>
    /// <param name="finalidade">Normal, Devolução, Complementar, Ajuste</param>
    /// <param name="exterior">true se a operação é com país estrangeiro (3xxx/7xxx)</param>
    public static IEnumerable<CfopOpcao> Sugerir(
        string? ufEmitente, string? ufDestino, TipoOperacao operacao,
        FinalidadeNota finalidade = FinalidadeNota.Normal, bool exterior = false)
    {
        var interestadual = !string.IsNullOrWhiteSpace(ufEmitente) && !string.IsNullOrWhiteSpace(ufDestino)
                            && !string.Equals(ufEmitente, ufDestino, StringComparison.OrdinalIgnoreCase);
        var saida = operacao == TipoOperacao.Saida;

        // Define primeiro dígito conforme matriz oficial:
        //   Saída intra=5, inter=6, exterior=7
        //   Entrada intra=1, inter=2, exterior=3
        char prefixo = (saida, interestadual, exterior) switch
        {
            (true,  _,    true)  => '7',
            (true,  true, false) => '6',
            (true,  false, _)    => '5',
            (false, _,    true)  => '3',
            (false, true, false) => '2',
            (false, false, _)    => '1'
        };

        // Lista candidatos do prefixo correto.
        var candidatos = _cfops
            .Where(kv => kv.Key[0] == prefixo)
            .Where(kv => saida ? kv.Value.Saida : kv.Value.Entrada)
            .Select(kv => new CfopOpcao(kv.Key, kv.Value.Descricao, kv.Value.Interestadual, kv.Value.Saida))
            .ToList();

        // Ordenação por relevância contextual:
        //   - Devolução prioriza CFOPs da família "2xx" (devolução compra) ou "20x" (devolução venda).
        //   - Saída normal prioriza venda de mercadoria adquirida (5/6 102) — caso mais frequente.
        return candidatos.OrderBy(c => RankCfop(c.Codigo, operacao, finalidade)).ThenBy(c => c.Codigo);
    }

    /// <summary>
    /// Rank de prioridade do CFOP no contexto da operação — menor = mais provável.
    /// Match exato por código para evitar ambiguidade entre CFOPs do mesmo grupo (ex.: 5101 vs 5102).
    /// </summary>
    private static int RankCfop(string cfop, TipoOperacao op, FinalidadeNota fin)
    {
        if (fin == FinalidadeNota.Devolucao)
        {
            return cfop switch
            {
                // SAÍDA — empresa devolve compra ao fornecedor
                "5202" or "6202" or "7202" => 0, // Devolução de compra para comercialização (top)
                "5201" or "6201" or "7201" => 1, // Devolução de compra para industrialização
                "5411" or "6411"           => 2, // Devolução compra industrialização ZFM

                // ENTRADA — cliente devolve venda à empresa
                "1202" or "2202" or "3202" => 0, // Devolução de venda de mercadoria
                "1201" or "2201" or "3201" => 1, // Devolução de venda de produção

                _ => 50
            };
        }

        // Operação normal — venda/compra de mercadoria adquirida é o caso esmagador.
        return cfop switch
        {
            // SAÍDA mais comum: revenda de mercadoria adquirida
            "5102" or "6102" or "7102" => 0,
            "5101" or "6101" or "7101" => 1,  // venda produção própria
            "5403" or "6403"           => 2,  // venda c/ ST
            "5405" or "6405"           => 3,  // venda mercadoria ST adquirida
            "5910" or "6910"           => 4,  // remessa bonificação
            "5911" or "6911"           => 5,  // remessa amostra
            "5551" or "6551"           => 6,  // venda ativo imobilizado

            // ENTRADA mais comum: compra para comercialização
            "1102" or "2102" or "3102" => 0,
            "1101" or "2101" or "3101" => 1,  // compra para industrialização
            "1403" or "2403"           => 2,  // compra c/ ST

            _ => 50
        };
    }
}

public record CfopInfo(string Descricao, bool Saida, bool Entrada, bool Interestadual);

/// <summary>
/// Representação leve de um CFOP para apresentação na UI.
/// </summary>
public record CfopOpcao(string Codigo, string Descricao, bool Interestadual, bool Saida)
{
    /// <summary>Texto formatado para exibição: "5.102 — Venda de mercadoria adquirida..."</summary>
    public string Display => $"{Codigo[..1]}.{Codigo[1..]} — {Descricao}";
}
