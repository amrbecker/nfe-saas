using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.Interfaces;

namespace NfeSaas.Infrastructure.Services;

/// <summary>
/// Carrega todos os XSDs em <c>{AppContext.BaseDirectory}/Schemas/</c> no startup
/// e valida XMLs gerados contra eles. Singleton — schemas são lidos uma vez.
/// Se a pasta estiver vazia ou ausente, validação XSD é pulada com aviso.
/// </summary>
public class XsdValidationService : IXsdValidationService
{
    private readonly XmlSchemaSet? _schemaSet;
    private readonly ILogger<XsdValidationService> _logger;
    private readonly List<string> _errosCarga = new();

    public bool TemSchemasCarregados { get; }
    public int TotalSchemasCarregados { get; }
    public IReadOnlyList<string> ErrosCarga => _errosCarga.AsReadOnly();

    public XsdValidationService(ILogger<XsdValidationService> logger)
    {
        _logger = logger;
        var pasta = LocalizarPastaSchemas();

        if (pasta == null || !Directory.Exists(pasta))
        {
            _logger.LogWarning("Pasta de schemas XSD não encontrada — validação XSD será pulada.");
            return;
        }

        var arquivos = Directory.GetFiles(pasta, "*.xsd");
        if (arquivos.Length == 0)
        {
            _logger.LogWarning("Nenhum arquivo .xsd em {Pasta} — validação XSD será pulada.", pasta);
            return;
        }

        _schemaSet = new XmlSchemaSet();
        _schemaSet.ValidationEventHandler += (_, e) =>
            _errosCarga.Add($"{e.Severity} ao carregar schema: {e.Message}");

        foreach (var arquivo in arquivos)
        {
            try
            {
                using var fs = File.OpenRead(arquivo);
                using var reader = XmlReader.Create(fs);
                _schemaSet.Add(null, reader);
            }
            catch (Exception ex)
            {
                _errosCarga.Add($"{Path.GetFileName(arquivo)}: {ex.Message}");
                _logger.LogWarning(ex, "Falha ao carregar XSD {Arquivo}", arquivo);
            }
        }

        try
        {
            _schemaSet.Compile();
            TotalSchemasCarregados = _schemaSet.Schemas().Count;
            TemSchemasCarregados = _errosCarga.Count == 0 && TotalSchemasCarregados > 0;
            _logger.LogInformation("XSDs carregados com sucesso: {Total} schemas a partir de {Pasta}",
                TotalSchemasCarregados, pasta);
        }
        catch (Exception ex)
        {
            _errosCarga.Add($"Falha na compilação dos schemas: {ex.Message}");
            _logger.LogError(ex, "Falha ao compilar XmlSchemaSet");
            _schemaSet = null;
        }
    }

    public XsdValidacaoResultado Validar(string xml)
    {
        var resultado = new XsdValidacaoResultado();

        if (!TemSchemasCarregados || _schemaSet == null)
        {
            resultado.Pulada = true;
            return resultado;
        }

        if (string.IsNullOrWhiteSpace(xml))
        {
            resultado.Erros.Add("XML vazio.");
            return resultado;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = _schemaSet,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
        };

        var erros = new List<string>();
        settings.ValidationEventHandler += (_, e) =>
        {
            var prefixo = e.Severity == XmlSeverityType.Error ? "[Erro]" : "[Aviso]";
            var loc = e.Exception != null
                ? $" linha {e.Exception.LineNumber} col {e.Exception.LinePosition}"
                : "";
            erros.Add($"{prefixo}{loc}: {e.Message}");
        };

        try
        {
            using var stringReader = new StringReader(xml);
            using var reader = XmlReader.Create(stringReader, settings);
            while (reader.Read()) { /* drain */ }
        }
        catch (XmlException ex)
        {
            erros.Add($"XML mal formado: {ex.Message} (linha {ex.LineNumber} col {ex.LinePosition})");
        }

        resultado.Erros = erros;
        resultado.Valido = !erros.Any(e => e.StartsWith("[Erro]") || e.StartsWith("XML mal"));
        return resultado;
    }

    private static string? LocalizarPastaSchemas()
    {
        // Procura ao lado do executável (Docker: /app/Schemas)
        var candidato = Path.Combine(AppContext.BaseDirectory, "Schemas");
        if (Directory.Exists(candidato)) return candidato;

        // Fallback: durante dev local pode ficar 1-2 níveis acima
        var atual = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 5 && atual != null; i++)
        {
            var t = Path.Combine(atual.FullName, "Schemas");
            if (Directory.Exists(t)) return t;
            atual = atual.Parent;
        }
        return null;
    }
}
