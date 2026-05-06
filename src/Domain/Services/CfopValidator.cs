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
}

public record CfopInfo(string Descricao, bool Saida, bool Entrada, bool Interestadual);
