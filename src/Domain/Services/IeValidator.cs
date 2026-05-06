using System.Text.RegularExpressions;

namespace NfeSaas.Domain.Services;

public static class IeValidator
{
    // Regex patterns per UF. "ISENTO" always accepted for non-contributors.
    private static readonly Dictionary<string, string> _patterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AC"] = @"^\d{13}$",
        ["AL"] = @"^24\d{7}$",
        ["AM"] = @"^\d{9}$",
        ["AP"] = @"^03\d{9}$",
        ["BA"] = @"^\d{8,9}$",
        ["CE"] = @"^\d{9}$",
        ["DF"] = @"^07\d{11}$",
        ["ES"] = @"^\d{9}$",
        ["GO"] = @"^1[0-9]\d{7}$",
        ["MA"] = @"^12\d{7}$",
        ["MG"] = @"^\d{13}$",
        ["MS"] = @"^28\d{7}$",
        ["MT"] = @"^\d{11}$",
        ["PA"] = @"^15\d{7}$",
        ["PB"] = @"^\d{9}$",
        ["PE"] = @"^\d{9}$|^\d{14}$",
        ["PI"] = @"^\d{9}$",
        ["PR"] = @"^\d{8}-\d{1}$|^\d{9}$",
        ["RJ"] = @"^\d{8}$",
        ["RN"] = @"^\d{9,10}$",
        ["RO"] = @"^\d{14}$",
        ["RR"] = @"^24\d{7}$",
        ["RS"] = @"^\d{10}$",
        ["SC"] = @"^\d{9}$",
        ["SE"] = @"^\d{9}$",
        ["SP"] = @"^\d{12}$|^P\d{8}$",
        ["TO"] = @"^\d{9,11}$",
    };

    public static bool Validar(string? ie, string? uf)
    {
        if (string.IsNullOrWhiteSpace(ie) || string.IsNullOrWhiteSpace(uf)) return false;
        if (ie.Trim().Equals("ISENTO", StringComparison.OrdinalIgnoreCase)) return true;

        if (!_patterns.TryGetValue(uf.Trim().ToUpper(), out var pattern)) return false;

        var digits = new string(ie.Where(c => char.IsDigit(c) || c == '-' || char.IsLetter(c)).ToArray());
        return Regex.IsMatch(digits.Trim(), pattern, RegexOptions.IgnoreCase);
    }

    public static bool UfValida(string? uf)
    {
        if (string.IsNullOrWhiteSpace(uf)) return false;
        return _patterns.ContainsKey(uf.Trim().ToUpper());
    }

    public static IReadOnlyCollection<string> UfsValidas() => _patterns.Keys.ToList().AsReadOnly();
}
