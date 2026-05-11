namespace NfeSaas.Domain.Services;

public static class NcmValidator
{
    public static bool Validar(string? ncm)
    {
        if (string.IsNullOrWhiteSpace(ncm)) return false;
        var digitos = new string(ncm.Where(char.IsDigit).ToArray());
        return digitos.Length == 8;
    }

    public static string ApenasDigitos(string? ncm) =>
        ncm == null ? "" : new string(ncm.Where(char.IsDigit).ToArray());
}

public static class CnaeValidator
{
    public static bool Validar(string? cnae)
    {
        if (string.IsNullOrWhiteSpace(cnae)) return false;
        var digitos = new string(cnae.Where(char.IsDigit).ToArray());
        return digitos.Length == 7;
    }

    public static string ApenasDigitos(string? cnae) =>
        cnae == null ? "" : new string(cnae.Where(char.IsDigit).ToArray());
}

public static class GtinValidator
{
    // Aceita GTIN-8, 12, 13 ou 14. Validação por algoritmo MOD10 (check digit).
    public static bool Validar(string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin)) return false;
        var digitos = new string(gtin.Where(char.IsDigit).ToArray());
        if (digitos.Length is not (8 or 12 or 13 or 14)) return false;

        var soma = 0;
        for (var i = 0; i < digitos.Length - 1; i++)
        {
            var d = digitos[i] - '0';
            var distanciaDoFinal = digitos.Length - 1 - i;
            var peso = distanciaDoFinal % 2 == 1 ? 3 : 1;
            soma += d * peso;
        }
        var dv = (10 - (soma % 10)) % 10;
        return dv == digitos[^1] - '0';
    }
}
