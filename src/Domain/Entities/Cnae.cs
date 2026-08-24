namespace NfeSaas.Domain.Entities;

/// <summary>
/// Classificação Nacional de Atividades Econômicas — tabela oficial mantida pelo IBGE/CONCLA.
/// Tabela global (sem multi-tenant): mesma referência para todos os emitentes.
/// </summary>
public class Cnae
{
    public string Codigo { get; private set; } = null!;       // 7 dígitos, chave primária
    public string Descricao { get; private set; } = null!;
    public string? Secao { get; private set; }                // letra da seção (ex.: "H")
    public string? Divisao { get; private set; }               // 2 primeiros dígitos
    public bool Ativo { get; private set; } = true;
    public DateTime AtualizadoEm { get; private set; } = DateTime.UtcNow;

    protected Cnae() { }

    public static Cnae Criar(string codigo, string descricao, string? secao = null, string? divisao = null)
    {
        var digitos = new string(codigo.Where(char.IsDigit).ToArray());
        if (digitos.Length != 7)
            throw new ArgumentException("Código CNAE deve ter 7 dígitos.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        return new Cnae
        {
            Codigo = digitos,
            Descricao = descricao.Trim(),
            Secao = secao,
            Divisao = divisao ?? digitos[..2],
            AtualizadoEm = DateTime.UtcNow
        };
    }
}
