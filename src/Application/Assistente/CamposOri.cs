namespace NfeSaas.Application.Assistente;

/// <summary>IDs canônicos de campos (docs/assistente/CONTRATOS_TECNICOS.md §3). Usado pela API e pela WebUI.</summary>
public static class CamposOri
{
    /// <summary>Campos cujo VALOR nunca é capturado nem enviado — só o id do campo e, quando houver, o resultado de validação.</summary>
    public static readonly IReadOnlySet<string> Sensiveis = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "cpf_cnpj", "ie", "razao_social", "logradouro", "email", "telefone", "senha_certificado", "csc_token", "csc", "certificado"
    };

    public static readonly IReadOnlySet<string> Fiscais = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "cfop", "cst", "csosn", "ncm", "cest", "natureza_operacao", "cst_ibs_cbs", "cclasstrib", "origem_mercadoria",
        "unidade", "gtin", "modalidade_frete", "forma_pagamento", "regime_tributario", "ind_ie", "codigo_municipio", "uf"
    };

    public static bool EhSensivel(string? campo) => campo != null && Sensiveis.Contains(campo);
}
