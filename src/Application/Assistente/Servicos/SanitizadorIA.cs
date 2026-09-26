using System.Text.RegularExpressions;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Assistente.Servicos;

/// <summary>
/// Minimização de dados (PESQUISA_REFINAMENTO.md §2.6): nenhum dado pessoal chega ao modelo. Documentos, e-mails,
/// telefones, CEPs e chaves de acesso viram marcadores ([CPF_1], [EMAIL_1]...); a tela reidrata com os valores reais.
/// Não mexe em códigos fiscais (NCM de 8 dígitos, CFOP, CST, cStat), valores monetários nem datas.
/// </summary>
public partial class SanitizadorIA : ISanitizadorIA
{
    // Ordem importa: padrões mais longos primeiro (chave de acesso contém sequências que parecem CNPJ/CPF).
    private static readonly (string Tipo, Regex Regex)[] Padroes =
    {
        ("CHAVE_ACESSO", RegexChave()),
        ("EMAIL", RegexEmail()),
        ("CNPJ", RegexCnpj()),
        ("CPF", RegexCpf()),
        ("TELEFONE", RegexTelefone()),
        ("CEP", RegexCep()),
    };

    public ResultadoSanitizacao Sanitizar(string? texto, IDictionary<string, string>? mapa = null)
    {
        mapa ??= new Dictionary<string, string>();
        if (string.IsNullOrEmpty(texto))
            return new ResultadoSanitizacao(texto ?? "", new Dictionary<string, string>(mapa), 0);

        var quantidade = 0;
        var resultado = texto;
        foreach (var (tipo, regex) in Padroes)
        {
            resultado = regex.Replace(resultado, m =>
            {
                var valor = m.Groups["v"].Success ? m.Groups["v"].Value : m.Value;
                var tipoReal = tipo;
                if (tipo == "CPF" && !CnpjValidator.ValidarCpf(valor) && PareceCelular(valor)) tipoReal = "TELEFONE";
                quantidade++;
                var marcador = MarcadorPara(tipoReal, valor, mapa);
                return m.Groups["v"].Success ? m.Value.Replace(valor, marcador) : marcador;
            });
        }

        // Rede de segurança: sequências longas de dígitos (≥ 11) que sobraram também saem.
        resultado = RegexDigitosLongos().Replace(resultado, m =>
        {
            quantidade++;
            return MarcadorPara("DOCUMENTO", m.Value, mapa);
        });

        return new ResultadoSanitizacao(resultado, new Dictionary<string, string>(mapa), quantidade);
    }

    public string Reidratar(string texto, IReadOnlyDictionary<string, string> substituicoes)
    {
        if (string.IsNullOrEmpty(texto) || substituicoes.Count == 0) return texto;
        // Marcadores mais longos primeiro ([CPF_10] antes de [CPF_1]).
        foreach (var (marcador, original) in substituicoes.OrderByDescending(kv => kv.Key.Length))
            texto = texto.Replace(marcador, original, StringComparison.Ordinal);
        return texto;
    }

    public AtributosPessoa DescreverPessoa(string? cpfCnpj, string? uf, string? inscricaoEstadual, int? indicadorIe = null)
    {
        var digitos = CnpjValidator.ApenasDigitos(cpfCnpj);
        var (tipo, valido) = digitos.Length switch
        {
            11 => ("PF", CnpjValidator.ValidarCpf(digitos)),
            14 => ("PJ", CnpjValidator.Validar(digitos)),
            0 => ("Nao_informado", false),
            _ => ("Estrangeiro_ou_invalido", false)
        };
        var iePresente = !string.IsNullOrWhiteSpace(inscricaoEstadual)
            && !inscricaoEstadual.Trim().Equals("ISENTO", StringComparison.OrdinalIgnoreCase);
        bool? ieValida = iePresente && !string.IsNullOrWhiteSpace(uf) && IeValidator.UfValida(uf)
            ? IeValidator.Validar(inscricaoEstadual, uf)
            : null;
        return new AtributosPessoa(tipo, valido, string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant(),
            iePresente, ieValida, indicadorIe);
    }

    private static string MarcadorPara(string tipo, string valor, IDictionary<string, string> mapa)
    {
        var chave = Normalizar(tipo, valor);
        foreach (var (marcador, original) in mapa)
            if (marcador.StartsWith($"[{tipo}_", StringComparison.Ordinal) && Normalizar(tipo, original) == chave)
                return marcador;

        var n = mapa.Keys.Count(k => k.StartsWith($"[{tipo}_", StringComparison.Ordinal)) + 1;
        var novo = $"[{tipo}_{n}]";
        mapa[novo] = valor;
        return novo;
    }

    private static string Normalizar(string tipo, string valor) =>
        tipo == "EMAIL" ? valor.Trim().ToLowerInvariant() : CnpjValidator.ApenasDigitos(valor);

    private static bool PareceCelular(string valor)
    {
        var d = CnpjValidator.ApenasDigitos(valor);
        return d.Length == 11 && d[2] == '9';
    }

    [GeneratedRegex(@"(?<!\d)(?:\d{4}[ .]?){10}\d{4}(?!\d)")]
    private static partial Regex RegexChave();

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
    private static partial Regex RegexEmail();

    [GeneratedRegex(@"(?<![\d./])\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}(?![\d/])")]
    private static partial Regex RegexCnpj();

    [GeneratedRegex(@"(?<![\d./])\d{3}\.?\d{3}\.?\d{3}-?\d{2}(?![\d.])")]
    private static partial Regex RegexCpf();

    // Exige DDD entre parênteses ou hífen no número, para não confundir com NCM/códigos.
    [GeneratedRegex(@"(?:\(\d{2}\)\s?9?\d{4}-?\d{4})|(?<![\d.])(?:\d{2}\s)?9?\d{4}-\d{4}(?![\d])")]
    private static partial Regex RegexTelefone();

    // CEP só com hífen ou precedido da palavra CEP (8 dígitos soltos podem ser NCM).
    [GeneratedRegex(@"(?<![\d.])\d{5}-\d{3}(?!\d)|(?i:cep)\s*:?\s*(?<v>\d{8})(?!\d)")]
    private static partial Regex RegexCep();

    [GeneratedRegex(@"(?<![\d\[_])\d{11,}(?![\d\]])")]
    private static partial Regex RegexDigitosLongos();
}
