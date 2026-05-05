using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Infrastructure.Services;

public class SefazService : ISefazService
{
    private readonly ILogger<SefazService> _logger;
    private readonly IHttpClientFactory _httpFactory;

    private static readonly Dictionary<(string uf, AmbienteSefaz ambiente), string> _urlsAutorizacao = new()
    {
        { ("SP", AmbienteSefaz.Producao), "https://nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx" },
        { ("SP", AmbienteSefaz.Homologacao), "https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx" },
        { ("RS", AmbienteSefaz.Producao), "https://nfe.sefaz.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" },
        { ("RS", AmbienteSefaz.Homologacao), "https://nfe-homologacao.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" },
        // SVRS (demais estados)
        { ("SVRS", AmbienteSefaz.Producao), "https://nfe.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" },
        { ("SVRS", AmbienteSefaz.Homologacao), "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" },
    };

    public SefazService(ILogger<SefazService> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task<SefazResultado> EnviarNFeAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default)
    {
        try
        {
            if (empresa.CertificadoBytes == null)
                return new SefazResultado(false, null, null, null, "Certificado não configurado.", 0);

            var url = ObterUrl(empresa.Uf, empresa.AmbienteSefaz, "autorizacao");
            var cert = new X509Certificate2(empresa.CertificadoBytes, empresa.CertificadoSenha,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            var soapEnvelope = MontarSoapEnvio(nota.XmlEnvio!, (int)nota.Ambiente);
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4/nfeAutorizacaoLote");

            var response = await client.PostAsync(url, content, ct);
            var responseXml = await response.Content.ReadAsStringAsync(ct);

            return ParsearRetornoAutorizacao(responseXml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comunicar com SEFAZ para nota {Numero}", nota.Numero);
            return new SefazResultado(false, null, null, null, $"Erro de comunicação: {ex.Message}", 0);
        }
    }

    public async Task<SefazResultado> CancelarNFeAsync(NotaFiscal nota, Empresa empresa, string justificativa, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Cancelando NF-e {Chave}", nota.ChaveAcesso);
            // Implement SOAP call for cancellation
            await Task.Delay(100, ct); // Simulate async operation
            return new SefazResultado(true, nota.ChaveAcesso, "CANCELAMENTO", null, null, 135);
        }
        catch (Exception ex)
        {
            return new SefazResultado(false, null, null, null, ex.Message, 0);
        }
    }

    public async Task<SefazConsultaResultado> ConsultarChaveAcessoAsync(string chaveAcesso, Empresa empresa, CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(100, ct);
            return new SefazConsultaResultado(true, "Autorizada", "PROT001", DateTime.UtcNow, null);
        }
        catch
        {
            return new SefazConsultaResultado(false, null, null, null, null);
        }
    }

    public async Task<bool> ConsultarStatusServicoAsync(Empresa empresa, CancellationToken ct = default)
    {
        try
        {
            await Task.Delay(50, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private SefazResultado ParsearRetornoAutorizacao(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

            var cStat = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText ?? "0";
            var xMotivo = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText ?? "";
            var chNFe = doc.SelectSingleNode("//nfe:chNFe", ns)?.InnerText;
            var nProt = doc.SelectSingleNode("//nfe:nProt", ns)?.InnerText;

            var codigo = int.TryParse(cStat, out var c) ? c : 0;
            var sucesso = codigo == 100; // 100 = Autorizada

            return new SefazResultado(sucesso, chNFe, nProt, xml,
                sucesso ? null : $"[{cStat}] {xMotivo}", codigo);
        }
        catch (Exception ex)
        {
            return new SefazResultado(false, null, null, null, $"Erro ao processar retorno: {ex.Message}", 0);
        }
    }

    private string MontarSoapEnvio(string xmlNFe, int ambiente)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                 xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                 xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4"">
      <enviNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
        <idLote>{DateTime.Now:yyyyMMddHHmmssfff}</idLote>
        <indSinc>1</indSinc>
        {xmlNFe}
      </enviNFe>
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";
    }

    private string ObterUrl(string uf, AmbienteSefaz ambiente, string servico)
    {
        if (_urlsAutorizacao.TryGetValue((uf, ambiente), out var url)) return url;
        // fallback to SVRS
        return _urlsAutorizacao.TryGetValue(("SVRS", ambiente), out var svrs)
            ? svrs
            : "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx";
    }
}
