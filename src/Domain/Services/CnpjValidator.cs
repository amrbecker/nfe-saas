namespace NfeSaas.Domain.Services;

public static class CnpjValidator
{
    private static readonly int[] _mult1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] _mult2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public static bool Validar(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj)) return false;

        cnpj = ApenasDigitos(cnpj);
        if (cnpj.Length != 14) return false;
        if (cnpj.Distinct().Count() == 1) return false;

        var soma = 0;
        for (var i = 0; i < 12; i++) soma += (cnpj[i] - '0') * _mult1[i];
        var r = soma % 11;
        var d1 = r < 2 ? 0 : 11 - r;

        soma = 0;
        for (var i = 0; i < 13; i++) soma += (cnpj[i] - '0') * _mult2[i];
        r = soma % 11;
        var d2 = r < 2 ? 0 : 11 - r;

        return (cnpj[12] - '0') == d1 && (cnpj[13] - '0') == d2;
    }

    public static bool ValidarCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        cpf = ApenasDigitos(cpf);
        if (cpf.Length != 11) return false;
        if (cpf.Distinct().Count() == 1) return false;

        var soma = 0;
        for (var i = 0; i < 9; i++) soma += (cpf[i] - '0') * (10 - i);
        var r = soma % 11;
        var d1 = r < 2 ? 0 : 11 - r;

        soma = 0;
        for (var i = 0; i < 10; i++) soma += (cpf[i] - '0') * (11 - i);
        r = soma % 11;
        var d2 = r < 2 ? 0 : 11 - r;

        return (cpf[9] - '0') == d1 && (cpf[10] - '0') == d2;
    }

    public static string ApenasDigitos(string? valor) =>
        valor == null ? "" : new string(valor.Where(char.IsDigit).ToArray());

    public static string FormatarCnpj(string cnpj)
    {
        cnpj = ApenasDigitos(cnpj);
        if (cnpj.Length != 14) return cnpj;
        return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}";
    }
}
