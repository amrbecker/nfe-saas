using System.Text;
using NfeSaas.Application.Assistente.Ia;

namespace NfeSaas.Infrastructure.Assistente;

/// <summary>Lê docs/assistente/prompt/sistema.txt, embutido como recurso "prompt/sistema.txt".</summary>
public class PromptSistemaEmbutido : IPromptSistema
{
    private static readonly Lazy<string> _texto = new(() =>
    {
        var assembly = typeof(PromptSistemaEmbutido).Assembly;
        var nome = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.Replace('\\', '/').Equals("prompt/sistema.txt", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Recurso prompt/sistema.txt não encontrado no assembly da Infrastructure.");
        using var stream = assembly.GetManifestResourceStream(nome)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        // Normaliza fim de linha para o prefixo ser byte-idêntico em qualquer SO de build.
        return reader.ReadToEnd().Replace("\r\n", "\n").Trim();
    });

    public string Texto => _texto.Value;
}
