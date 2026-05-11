namespace NfeSaas.Domain.Services;

/// <summary>
/// Validação cruzada CFOP × NCM.
///
/// Alguns CFOPs (5403/5405/6403/6405) são exclusivos de produtos sujeitos à
/// Substituição Tributária do ICMS — em outras palavras, NCMs que possuem CEST.
/// Usar um CFOP de ST com um NCM que não tem CEST (ou vice-versa) é a 3ª causa
/// mais comum de rejeição da SEFAZ na NF-e v4.00 (códigos de erro 4920, 6019,
/// 6122 da Nota Técnica 2016.003 e posteriores).
///
/// Esta classe **não bloqueia** a emissão — apenas produz mensagens de aviso
/// que a UI exibe inline. Há cenários legítimos de exceção (Convênios específicos,
/// regimes especiais por UF) onde a SEFAZ aceita a combinação.
/// </summary>
public static class CfopNcmConsistencia
{
    /// <summary>
    /// CFOPs que exigem produto sob regime de Substituição Tributária do ICMS.
    /// </summary>
    private static readonly HashSet<string> CfopsSubstituicaoTributaria = new()
    {
        // Saída — operação com ST
        "5401", "5402", "5403", "5405", "5410", "5411", "5412", "5413", "5414",
        "6401", "6402", "6403", "6404", "6405", "6410", "6411", "6412", "6413", "6414",
        // Entrada — recebimento com ST cobrado
        "1401", "1403", "1406", "1407", "1409", "1410", "1411", "1414", "1415",
        "2401", "2403", "2406", "2407", "2408", "2410", "2411", "2414", "2415"
    };

    /// <summary>
    /// Verifica se o CFOP pertence à família de Substituição Tributária.
    /// </summary>
    public static bool EhCfopSubstituicaoTributaria(string? cfop)
    {
        if (string.IsNullOrWhiteSpace(cfop)) return false;
        return CfopsSubstituicaoTributaria.Contains(cfop.Trim());
    }

    /// <summary>
    /// Resultado de uma verificação de consistência.
    /// </summary>
    public record Resultado(bool Consistente, string? Mensagem);

    /// <summary>
    /// Verifica se um CFOP é compatível com um NCM (considerando se o NCM exige CEST).
    /// </summary>
    /// <param name="cfop">Código CFOP (4 dígitos)</param>
    /// <param name="ncmExigeCest">true se o NCM está na tabela CEST (Convênio Confaz 142/2018)</param>
    /// <param name="cest">Código CEST informado no item, se houver (7 dígitos)</param>
    public static Resultado Verificar(string? cfop, bool ncmExigeCest, string? cest = null)
    {
        var cfopST = EhCfopSubstituicaoTributaria(cfop);
        var temCest = !string.IsNullOrWhiteSpace(cest)
                      && cest.Where(char.IsDigit).Count() == 7;

        // CFOP de ST + NCM sem ST + sem CEST informado → muito provável rejeição.
        if (cfopST && !ncmExigeCest && !temCest)
            return new Resultado(false,
                $"CFOP {cfop} é de Substituição Tributária, mas o NCM informado não consta " +
                "na tabela CEST. Verifique se a operação realmente envolve ST.");

        // CFOP normal + NCM com ST → o emitente pode estar deixando de aplicar ST devida.
        if (!cfopST && ncmExigeCest)
            return new Resultado(false,
                "Este NCM está sujeito à Substituição Tributária (consta no CEST). " +
                $"O CFOP {cfop} não pertence à família de ST — confirme se a operação " +
                "está enquadrada em alguma exceção.");

        // CFOP de ST + sem CEST mas NCM exige CEST → falta preencher CEST.
        if (cfopST && ncmExigeCest && !temCest)
            return new Resultado(false,
                "Operação de Substituição Tributária sem CEST informado. " +
                "Preencha o CEST do item.");

        return new Resultado(true, null);
    }
}
