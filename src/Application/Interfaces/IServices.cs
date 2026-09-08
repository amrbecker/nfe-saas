using NfeSaas.Domain.Entities;

namespace NfeSaas.Application.Interfaces;

public interface ISefazService
{
    Task<SefazResultado> EnviarNFeAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default);
    Task<SefazResultado> CancelarNFeAsync(NotaFiscal nota, Empresa empresa, string xmlEventoAssinado, CancellationToken ct = default);
    Task<SefazConsultaResultado> ConsultarChaveAcessoAsync(string chaveAcesso, Empresa empresa, CancellationToken ct = default);
    Task<bool> ConsultarStatusServicoAsync(Empresa empresa, CancellationToken ct = default);

    // Eventos fiscais
    Task<SefazResultado> EnviarEventoCceAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default);
    Task<SefazResultado> EnviarInutilizacaoAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default);
    Task<SefazResultado> EnviarManifestacaoAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default);
}

public record SefazResultado(
    bool Sucesso,
    string? ChaveAcesso,
    string? Protocolo,
    string? XmlRetorno,
    string? MensagemErro,
    int CodigoRetorno);

public record SefazConsultaResultado(
    bool Encontrada,
    string? Situacao,
    string? Protocolo,
    DateTime? DataAutorizacao,
    string? XmlRetorno);

public interface IXsdValidationService
{
    bool TemSchemasCarregados { get; }
    int TotalSchemasCarregados { get; }
    IReadOnlyList<string> ErrosCarga { get; }
    XsdValidacaoResultado Validar(string xml);
}

public class XsdValidacaoResultado
{
    public bool Valido { get; set; }
    public bool Pulada { get; set; }
    public List<string> Erros { get; set; } = new();
}

public interface IXmlNFeService
{
    string GerarXmlNFe(NotaFiscal nota, Empresa empresa);
    string AssinarXml(string xml, byte[] certificadoBytes, string senha);
    string GerarXmlCancelamento(string chaveAcesso, string protocolo, string justificativa, Empresa empresa);
    bool ValidarXml(string xml, out IEnumerable<string> erros);

    string GerarXmlCce(string chaveAcesso, int sequencial, string correcao, Empresa empresa);
    string GerarXmlInutilizacao(Empresa empresa, int ano, NfeSaas.Domain.Enums.TipoNota tipo, int serie, int numIni, int numFin, string justificativa);
    string GerarXmlManifestacao(string chaveAcesso, NfeSaas.Domain.Enums.TipoEventoFiscal tipo, string justificativa, Empresa empresa);

    // Assinatura digital de eventos fiscais (CC-e, Manifestação) e Inutilização.
    // Estrutura SEFAZ: para evento o <Signature> vai como filho de <evento> (irmão de <infEvento>);
    // para inutilização vai como filho de <inutNFe> (irmão de <infInut>).
    string AssinarEvento(string xml, byte[] certificadoBytes, string senha);
    string AssinarInutilizacao(string xml, byte[] certificadoBytes, string senha);
    string AssinarCancelamento(string xml, byte[] certificadoBytes, string senha);
}

public interface IDanfeService
{
    Task<byte[]> GerarDanfePdfAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default);
    Task<byte[]> GerarDanfeNFCePdfAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default);
}

public interface ICertificadoService
{
    CertificadoInfo ValidarCertificado(byte[] bytes, string senha);
    byte[] ExportarPublicKey(byte[] bytes, string senha);
}

public record CertificadoInfo(
    bool Valido,
    string? Cnpj,
    string? NomeTitular,
    DateTime Validade,
    string? MensagemErro);

public interface IImpostoCalculoService
{
    ImpostoResultado CalcularIcms(decimal valorProduto, decimal aliquota, decimal? percentualReducao = null);
    ImpostoResultado CalcularPis(decimal valorProduto, decimal aliquota);
    ImpostoResultado CalcularCofins(decimal valorProduto, decimal aliquota);
    ImpostoResultado CalcularIcmsSt(decimal valorProduto, decimal mva, decimal aliquotaInterna, decimal aliquotaInterestadual);
    ImpostoResultado CalcularIpi(decimal valorProduto, decimal aliquota);
    ImpostoResultado CalcularFcp(decimal baseCalculoIcms, decimal aliquota);
    DifalResultado CalcularDifal(decimal valorProduto, decimal aliquotaInternaUfDestino, decimal aliquotaInterestadual);
    IbsCbsResultado CalcularIbsCbs(decimal valorProduto,
        decimal aliquotaIbsUf = AliquotasIbsCbsVigentes.IbsUf,
        decimal aliquotaIbsMun = AliquotasIbsCbsVigentes.IbsMun,
        decimal aliquotaCbs = AliquotasIbsCbsVigentes.Cbs);
}

public record DifalResultado(decimal BaseCalculo, decimal AliquotaInterna, decimal AliquotaInterestadual, decimal ValorUfDestino, decimal ValorUfRemetente);

public record ImpostoResultado(decimal BaseCalculo, decimal Aliquota, decimal Valor);

public record IbsCbsResultado(decimal BaseCalculo,
    decimal AliquotaIbsUf, decimal ValorIbsUf,
    decimal AliquotaIbsMun, decimal ValorIbsMun,
    decimal ValorIbs,
    decimal AliquotaCbs, decimal ValorCbs);

/// <summary>
/// Alíquotas do IBS/CBS vigentes na fase de teste da Reforma Tributária (LC 214/2025).
/// 2026: 1% total "por fora" (0,90% CBS + 0,10% IBS, dividido 0,08% UF + 0,02% Município),
/// dispensado de recolhimento para quem cumprir as obrigações acessórias (art. 348, §1º da LC 214/2025).
/// O cronograma legal aumenta essas alíquotas a partir de 2027 — revisar estes valores quando a
/// próxima fase for publicada (acompanhar Notas Técnicas em nfe.fazenda.gov.br).
/// </summary>
public static class AliquotasIbsCbsVigentes
{
    public const decimal IbsUf = 0.08m;
    public const decimal IbsMun = 0.02m;
    public const decimal Cbs = 0.90m;
}

public interface ITokenService
{
    string GerarAccessToken(Guid usuarioId, string email, string role, Guid escritorioId, Guid? empresaId = null);
    string GerarRefreshToken();
    Guid? ObterUsuarioIdDoToken(string token);
}

public interface IEmailService
{
    Task<bool> EnviarNFeAsync(string destinatario, string chaveAcesso, byte[] xmlBytes, byte[] danfeBytes, CancellationToken ct = default);
}

public interface ICepValidationService
{
    Task<CepInfo?> ConsultarAsync(string cep, CancellationToken ct = default);
    bool FormatoValido(string? cep);
}

public record CepInfo(string Cep, string Logradouro, string Bairro, string Cidade, string Uf, string CodigoMunicipio);

public interface IAuditService
{
    Task RegistrarAsync(Guid empresaId, string acao, Guid? usuarioId = null,
        string? chaveNfe = null, string? detalhes = null, string? ipOrigem = null,
        CancellationToken ct = default);
}
