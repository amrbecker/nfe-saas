namespace NfeSaas.Domain.Entities;

/// <summary>
/// Nomenclatura Comum do Mercosul — tabela oficial mantida pela Receita Federal/MDIC.
/// Tabela global (sem multi-tenant): mesma referência para todos os emitentes.
/// Atualizada periodicamente via worker; <see cref="VersaoTabela"/> identifica a publicação.
/// </summary>
public class Ncm
{
    public string Codigo { get; private set; } = null!;       // 8 dígitos, chave primária
    public string Descricao { get; private set; } = null!;
    public string? CategoriaCapitulo { get; private set; }    // 1º par de dígitos (cap. 01–99)
    public string? Posicao { get; private set; }              // 4 primeiros dígitos
    public decimal? AliquotaIpiPadrao { get; private set; }
    public bool ExigeCest { get; private set; }
    public bool Ativo { get; private set; } = true;
    public string VersaoTabela { get; private set; } = "";    // ex.: "2024-12"
    public DateTime AtualizadoEm { get; private set; } = DateTime.UtcNow;

    protected Ncm() { }

    public static Ncm Criar(string codigo, string descricao, string versaoTabela,
        string? capitulo = null, string? posicao = null,
        decimal? aliquotaIpi = null, bool exigeCest = false)
    {
        var digitos = new string(codigo.Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
            throw new ArgumentException("Código NCM deve ter 8 dígitos.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        return new Ncm
        {
            Codigo = digitos,
            Descricao = descricao.Trim(),
            CategoriaCapitulo = capitulo ?? digitos[..2],
            Posicao = posicao ?? digitos[..4],
            AliquotaIpiPadrao = aliquotaIpi,
            ExigeCest = exigeCest,
            VersaoTabela = versaoTabela,
            AtualizadoEm = DateTime.UtcNow
        };
    }

    public void Atualizar(string descricao, string versaoTabela,
        decimal? aliquotaIpi = null, bool? exigeCest = null)
    {
        Descricao = descricao.Trim();
        VersaoTabela = versaoTabela;
        if (aliquotaIpi.HasValue) AliquotaIpiPadrao = aliquotaIpi;
        if (exigeCest.HasValue) ExigeCest = exigeCest.Value;
        AtualizadoEm = DateTime.UtcNow;
    }

    public void Desativar() { Ativo = false; AtualizadoEm = DateTime.UtcNow; }
    public void Ativar()    { Ativo = true;  AtualizadoEm = DateTime.UtcNow; }
}
