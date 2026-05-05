using NfeSaas.Domain.Entities;

namespace NfeSaas.Application.Interfaces;

public interface ISefazService
{
    Task<SefazResultado> EnviarNFeAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default);
    Task<SefazResultado> CancelarNFeAsync(NotaFiscal nota, Empresa empresa, string justificativa, CancellationToken ct = default);
    Task<SefazConsultaResultado> ConsultarChaveAcessoAsync(string chaveAcesso, Empresa empresa, CancellationToken ct = default);
    Task<bool> ConsultarStatusServicoAsync(Empresa empresa, CancellationToken ct = default);
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

public interface IXmlNFeService
{
    string GerarXmlNFe(NotaFiscal nota, Empresa empresa);
    string AssinarXml(string xml, byte[] certificadoBytes, string senha);
    string GerarXmlCancelamento(string chaveAcesso, string justificativa, Empresa empresa);
    bool ValidarXml(string xml, out IEnumerable<string> erros);
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
}

public record ImpostoResultado(decimal BaseCalculo, decimal Aliquota, decimal Valor);

public interface ITokenService
{
    string GerarAccessToken(Guid usuarioId, string email, string role, Guid escritorioId, Guid? empresaId = null);
    string GerarRefreshToken();
    Guid? ObterUsuarioIdDoToken(string token);
}

public interface IEmailService
{
    Task EnviarNFeAsync(string destinatario, string chaveAcesso, byte[] xmlBytes, byte[] danfeBytes, CancellationToken ct = default);
}
