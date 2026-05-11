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

    // SVC-AN contingency URLs (SEFAZ Virtual - Ambiente Nacional)
    private static readonly Dictionary<AmbienteSefaz, string> _urlsSvcAn = new()
    {
        { AmbienteSefaz.Producao, "https://www.svc.fazenda.gov.br/NFeAutorizacao4/NFeAutorizacao4.asmx" },
        { AmbienteSefaz.Homologacao, "https://hom.svc.fazenda.gov.br/NFeAutorizacao4/NFeAutorizacao4.asmx" },
    };

    // SVC-RS contingency URLs (states using RS as contingency)
    private static readonly Dictionary<AmbienteSefaz, string> _urlsSvcRs = new()
    {
        { AmbienteSefaz.Producao, "https://nfe.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" },
        { AmbienteSefaz.Homologacao, "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" },
    };

    // States that use SVC-RS as contingency (others use SVC-AN)
    private static readonly HashSet<string> _estadosSvcRs = ["AM", "BA", "CE", "GO", "MA", "MS", "MT", "PA", "PE", "PI", "RN", "RS"];

    public SefazService(ILogger<SefazService> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task<SefazResultado> EnviarNFeAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default)
    {
        if (empresa.CertificadoBytes == null)
            return new SefazResultado(false, null, null, null, "Certificado não configurado.", 0);

        // Try primary SEFAZ first
        var resultado = await TentarEnvioAsync(nota, empresa, ObterUrl(empresa.Uf, empresa.AmbienteSefaz, "autorizacao"), ct);

        if (!resultado.Sucesso && IsErroConexao(resultado))
        {
            // Primary SEFAZ unavailable — fall back to contingency (SVC-AN or SVC-RS)
            _logger.LogWarning("SEFAZ primária indisponível para nota {Numero}. Tentando contingência.", nota.Numero);
            var urlContingencia = ObterUrlContingencia(empresa.Uf, empresa.AmbienteSefaz);
            resultado = await TentarEnvioAsync(nota, empresa, urlContingencia, ct);

            if (!resultado.Sucesso && IsErroConexao(resultado))
                return new SefazResultado(false, null, null, null,
                    "SEFAZ indisponível. Nota salva em contingência. Retransmitir quando o serviço retornar.", -1);
        }

        return resultado;
    }

    private async Task<SefazResultado> TentarEnvioAsync(NotaFiscal nota, Empresa empresa, string url, CancellationToken ct)
    {
        try
        {
            var cert = new X509Certificate2(empresa.CertificadoBytes!, empresa.CertificadoSenha,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            var soapEnvelope = MontarSoapEnvio(nota.XmlEnvio!, (int)nota.Ambiente);
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4/nfeAutorizacaoLote");

            var response = await client.PostAsync(url, content, ct);
            var responseXml = await response.Content.ReadAsStringAsync(ct);

            return ParsearRetornoAutorizacao(responseXml);
        }
        catch (TaskCanceledException)
        {
            return new SefazResultado(false, null, null, null, "Timeout na comunicação com SEFAZ.", -2);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Erro de conexão com SEFAZ na URL {Url}", url);
            return new SefazResultado(false, null, null, null, "Falha na conexão com a SEFAZ. Verifique a disponibilidade do serviço.", -2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comunicar com SEFAZ para nota {Numero}", nota.Numero);
            return new SefazResultado(false, null, null, null, "Erro na comunicação com a SEFAZ.", 0);
        }
    }

    private static bool IsErroConexao(SefazResultado r) => r.CodigoRetorno is -1 or -2;

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
            _logger.LogError(ex, "Erro ao cancelar NF-e {Chave}", nota.ChaveAcesso);
            return new SefazResultado(false, null, null, null, "Erro ao processar o cancelamento da NF-e.", 0);
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
            _logger.LogError(ex, "Erro ao processar retorno SEFAZ");
            return new SefazResultado(false, null, null, null, "Erro ao processar resposta da SEFAZ. O formato do retorno não era esperado.", 0);
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
        return _urlsAutorizacao.TryGetValue(("SVRS", ambiente), out var svrs)
            ? svrs
            : "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx";
    }

    private string ObterUrlContingencia(string uf, AmbienteSefaz ambiente)
    {
        if (_estadosSvcRs.Contains(uf.ToUpper()))
            return _urlsSvcRs.TryGetValue(ambiente, out var rsUrl) ? rsUrl : _urlsSvcRs[AmbienteSefaz.Homologacao];

        return _urlsSvcAn.TryGetValue(ambiente, out var anUrl) ? anUrl : _urlsSvcAn[AmbienteSefaz.Homologacao];
    }

    // Stubs de eventos fiscais. Em produção, cada um chama seu próprio web service SEFAZ
    // (RecepcaoEvento para CC-e e Manifestação; NfeInutilizacao para Inutilização).
    // Aqui registramos no log e retornamos sucesso em homologação/dev.
    public async Task<SefazResultado> EnviarEventoCceAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        if (empresa.AmbienteSefaz == AmbienteSefaz.Producao)
            return new SefazResultado(false, evento.ChaveAcesso, null, null,
                "Envio real de CC-e à SEFAZ ainda não implementado em produção.", 0);
        _logger.LogInformation("CC-e simulada para chave {Chave} sequencial {Seq}", evento.ChaveAcesso, evento.SequencialCce);
        return new SefazResultado(true, evento.ChaveAcesso, $"CCE{DateTime.Now:yyMMddHHmmss}", null, null, 135);
    }

    public async Task<SefazResultado> EnviarInutilizacaoAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        if (empresa.AmbienteSefaz == AmbienteSefaz.Producao)
            return new SefazResultado(false, null, null, null,
                "Envio real de inutilização à SEFAZ ainda não implementado em produção.", 0);
        _logger.LogInformation("Inutilização simulada — empresa {Cnpj} série {Serie} {Ini}-{Fin}",
            empresa.Cnpj, evento.SerieInutilizacao, evento.NumeroInicialInutilizacao, evento.NumeroFinalInutilizacao);
        return new SefazResultado(true, null, $"INU{DateTime.Now:yyMMddHHmmss}", null, null, 102);
    }

    public async Task<SefazResultado> EnviarManifestacaoAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        if (empresa.AmbienteSefaz == AmbienteSefaz.Producao)
            return new SefazResultado(false, evento.ChaveAcesso, null, null,
                "Envio real de manifestação à SEFAZ ainda não implementado em produção.", 0);
        _logger.LogInformation("Manifestação simulada — chave {Chave} tipo {Tipo}", evento.ChaveAcesso, evento.Tipo);
        return new SefazResultado(true, evento.ChaveAcesso, $"MAN{DateTime.Now:yyMMddHHmmss}", null, null, 135);
    }

    public async Task<bool> ConsultarStatusServicoAsync(Empresa empresa, CancellationToken ct = default)
    {
        // Override the existing stub with a real connectivity check
        try
        {
            if (empresa.CertificadoBytes == null) return false;

            var cert = new X509Certificate2(empresa.CertificadoBytes, empresa.CertificadoSenha,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            var url = ObterUrl(empresa.Uf, empresa.AmbienteSefaz, "status");
            var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
