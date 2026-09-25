using NfeSaas.Application.Assistente;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Infrastructure.Assistente;

/// <summary>Regras de governança da base (BASE_CONHECIMENTO.md §4) — roda nos testes e bloqueia merge de artigo inválido.</summary>
public static class BaseConhecimentoValidador
{
    public static readonly HashSet<string> Status = new() { "rascunho", "revisado", "publicado", "em_revisao" };

    // IDs canônicos — docs/assistente/CONTRATOS_TECNICOS.md §3.
    public static readonly HashSet<string> Campos = new()
    {
        "cfop", "cst", "csosn", "ncm", "cest", "natureza_operacao", "cst_ibs_cbs", "cclasstrib", "origem_mercadoria",
        "unidade", "quantidade", "valor_unitario", "gtin", "modalidade_frete", "forma_pagamento", "informacoes_adicionais",
        "serie", "numero", "regime_tributario", "ambiente", "cep", "codigo_municipio", "uf", "ind_ie",
        "certificado", "csc", "cpf_cnpj", "ie", "razao_social", "logradouro", "email", "telefone", "senha_certificado", "csc_token"
    };

    public static readonly HashSet<string> Telas = new()
    {
        "dashboard", "emitir-nfe", "notas", "nota-detalhe", "produtos", "clientes", "empresa", "empresas",
        "configuracao-inicial", "certificado", "inutilizacoes", "usuarios", "escritorio-como-empresa", "lembretes", "login"
    };

    /// <summary>Lista de erros ("id: problema"). Vazia = base válida.</summary>
    public static List<string> Validar(IEnumerable<(string Caminho, ArtigoKb Artigo)> artigos)
    {
        var erros = new List<string>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (caminho, a) in artigos)
        {
            var esperado = BaseConhecimentoParser.IdDoCaminho(caminho);
            void Erro(string msg) => erros.Add($"{a.Id}: {msg}");

            if (!ids.Add(a.Id)) Erro("id duplicado");
            if (!string.Equals(a.Id, esperado, StringComparison.Ordinal)) Erro($"id deve ser igual ao caminho ('{esperado}')");
            if (string.IsNullOrWhiteSpace(a.Titulo)) Erro("titulo obrigatório");
            if (string.IsNullOrWhiteSpace(a.Categoria)) Erro("categoria obrigatória");
            if (a.NivelFonte == NivelFonte.N4SemFonte) Erro("nivel_fonte ausente ou inválido (N1, N2, N3 ou sistema)");
            if (!Status.Contains(a.Status)) Erro($"status inválido '{a.Status}'");
            if (string.IsNullOrWhiteSpace(a.ResumoCurto)) Erro("resumo_curto obrigatório");
            else if (a.ResumoCurto.Length > 280) Erro($"resumo_curto com {a.ResumoCurto.Length} caracteres (máx. 280)");
            if (a.NivelFonte == NivelFonte.GuiaDoSistema && a.Categoria != "sistema") Erro("nível 'sistema' só na categoria sistema");
            if (a.Categoria == "sistema" && a.NivelFonte != NivelFonte.GuiaDoSistema) Erro("categoria sistema exige nivel_fonte: sistema");
            foreach (var c in a.CamposRelacionados.Where(c => !Campos.Contains(c))) Erro($"campo não canônico '{c}'");
            foreach (var t in a.Telas.Where(t => !Telas.Contains(t))) Erro($"tela não canônica '{t}'");
            if (a.Status is "revisado" or "publicado" or "em_revisao")
            {
                if (a.Fontes.Count == 0) Erro("artigo revisado/publicado exige ao menos uma fonte");
                if (a.VerificadoEm == null) Erro("artigo revisado/publicado exige verificado_em");
                if (a.RevisarAte == null) Erro("artigo revisado/publicado exige revisar_ate");
                if (string.IsNullOrWhiteSpace(a.Curador)) Erro("artigo revisado/publicado exige curador");
            }
        }
        return erros;
    }
}
