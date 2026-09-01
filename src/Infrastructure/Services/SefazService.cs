using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Infrastructure.Services;

// Endpoints e mapeamento UF -> provedor verificados em 2026-08-18 contra o Portal Nacional da
// NFe (nfe.fazenda.gov.br), o portal da SVRS (dfe-portal.svrs.rs.gov.br) e a SEFAZ/SP.
// A procedência fica registrada aqui, junto das URLs, por ser dado operacional.
// Revalidar periodicamente: URLs de webservices de governo mudam sem aviso prévio ao contribuinte.
public class SefazService : ISefazService
{
    private readonly ILogger<SefazService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly bool _useRealWebservice;

    private enum Servico { Autorizacao, RecepcaoEvento, Inutilizacao, ConsultaProtocolo }

    // UFs com webservice próprio. Demais UFs caem no default SVRS (fallback histórico adotado
    // pela maioria dos estados sem infraestrutura própria). MA é caso especial: usa SVAN mesmo
    // como autorizador primário (não tem webservice próprio nem usa SVRS por padrão).
    private static readonly Dictionary<string, Dictionary<Servico, (string Producao, string Homologacao)>> _porUf = new()
    {
        ["AM"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefaz.am.gov.br/services2/services/NfeAutorizacao4", "https://homnfe.sefaz.am.gov.br/services2/services/NfeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefaz.am.gov.br/services2/services/RecepcaoEvento4", "https://homnfe.sefaz.am.gov.br/services2/services/RecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.sefaz.am.gov.br/services2/services/NfeInutilizacao4", "https://homnfe.sefaz.am.gov.br/services2/services/NfeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefaz.am.gov.br/services2/services/NfeConsulta4", "https://homnfe.sefaz.am.gov.br/services2/services/NfeConsulta4"),
        },
        ["BA"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefaz.ba.gov.br/webservices/NFeAutorizacao4/NFeAutorizacao4.asmx", "https://hnfe.sefaz.ba.gov.br/webservices/NFeAutorizacao4/NFeAutorizacao4.asmx"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefaz.ba.gov.br/webservices/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx", "https://hnfe.sefaz.ba.gov.br/webservices/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx"),
            [Servico.Inutilizacao] = ("https://nfe.sefaz.ba.gov.br/webservices/NFeInutilizacao4/NFeInutilizacao4.asmx", "https://hnfe.sefaz.ba.gov.br/webservices/NFeInutilizacao4/NFeInutilizacao4.asmx"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefaz.ba.gov.br/webservices/NFeConsultaProtocolo4/NFeConsultaProtocolo4.asmx", "https://hnfe.sefaz.ba.gov.br/webservices/NFeConsultaProtocolo4/NFeConsultaProtocolo4.asmx"),
        },
        ["GO"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefaz.go.gov.br/nfe/services/NFeAutorizacao4", "https://homolog.sefaz.go.gov.br/nfe/services/NFeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefaz.go.gov.br/nfe/services/NFeRecepcaoEvento4", "https://homolog.sefaz.go.gov.br/nfe/services/NFeRecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.sefaz.go.gov.br/nfe/services/NFeInutilizacao4", "https://homolog.sefaz.go.gov.br/nfe/services/NFeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefaz.go.gov.br/nfe/services/NFeConsultaProtocolo4", "https://homolog.sefaz.go.gov.br/nfe/services/NFeConsultaProtocolo4"),
        },
        ["MG"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.fazenda.mg.gov.br/nfe2/services/NFeAutorizacao4", "https://hnfe.fazenda.mg.gov.br/nfe2/services/NFeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.fazenda.mg.gov.br/nfe2/services/NFeRecepcaoEvento4", "https://hnfe.fazenda.mg.gov.br/nfe2/services/NFeRecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.fazenda.mg.gov.br/nfe2/services/NFeInutilizacao4", "https://hnfe.fazenda.mg.gov.br/nfe2/services/NFeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.fazenda.mg.gov.br/nfe2/services/NFeConsultaProtocolo4", "https://hnfe.fazenda.mg.gov.br/nfe2/services/NFeConsultaProtocolo4"),
        },
        ["MS"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefaz.ms.gov.br/ws/NFeAutorizacao4", "https://hom.nfe.sefaz.ms.gov.br/ws/NFeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefaz.ms.gov.br/ws/NFeRecepcaoEvento4", "https://hom.nfe.sefaz.ms.gov.br/ws/NFeRecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.sefaz.ms.gov.br/ws/NFeInutilizacao4", "https://hom.nfe.sefaz.ms.gov.br/ws/NFeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefaz.ms.gov.br/ws/NFeConsultaProtocolo4", "https://hom.nfe.sefaz.ms.gov.br/ws/NFeConsultaProtocolo4"),
        },
        ["MT"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefaz.mt.gov.br/nfews/v2/services/NfeAutorizacao4", "https://homologacao.sefaz.mt.gov.br/nfews/v2/services/NfeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefaz.mt.gov.br/nfews/v2/services/RecepcaoEvento4", "https://homologacao.sefaz.mt.gov.br/nfews/v2/services/RecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.sefaz.mt.gov.br/nfews/v2/services/NfeInutilizacao4", "https://homologacao.sefaz.mt.gov.br/nfews/v2/services/NfeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefaz.mt.gov.br/nfews/v2/services/NfeConsulta4", "https://homologacao.sefaz.mt.gov.br/nfews/v2/services/NfeConsulta4"),
        },
        ["PE"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefaz.pe.gov.br/nfe-service/services/NFeAutorizacao4", "https://nfehomolog.sefaz.pe.gov.br/nfe-service/services/NFeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefaz.pe.gov.br/nfe-service/services/NFeRecepcaoEvento4", "https://nfehomolog.sefaz.pe.gov.br/nfe-service/services/NFeRecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.sefaz.pe.gov.br/nfe-service/services/NFeInutilizacao4", "https://nfehomolog.sefaz.pe.gov.br/nfe-service/services/NFeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefaz.pe.gov.br/nfe-service/services/NFeConsultaProtocolo4", "https://nfehomolog.sefaz.pe.gov.br/nfe-service/services/NFeConsultaProtocolo4"),
        },
        ["PR"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefa.pr.gov.br/nfe/NFeAutorizacao4", "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeAutorizacao4"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefa.pr.gov.br/nfe/NFeRecepcaoEvento4", "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeRecepcaoEvento4"),
            [Servico.Inutilizacao] = ("https://nfe.sefa.pr.gov.br/nfe/NFeInutilizacao4", "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeInutilizacao4"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefa.pr.gov.br/nfe/NFeConsultaProtocolo4", "https://homologacao.nfe.sefa.pr.gov.br/nfe/NFeConsultaProtocolo4"),
        },
        ["RS"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx", "https://nfe-homologacao.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx"),
            [Servico.RecepcaoEvento] = ("https://nfe.sefazrs.rs.gov.br/ws/recepcaoevento/recepcaoevento4.asmx", "https://nfe-homologacao.sefazrs.rs.gov.br/ws/recepcaoevento/recepcaoevento4.asmx"),
            [Servico.Inutilizacao] = ("https://nfe.sefazrs.rs.gov.br/ws/nfeinutilizacao/nfeinutilizacao4.asmx", "https://nfe-homologacao.sefazrs.rs.gov.br/ws/nfeinutilizacao/nfeinutilizacao4.asmx"),
            [Servico.ConsultaProtocolo] = ("https://nfe.sefazrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx", "https://nfe-homologacao.sefazrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx"),
        },
        ["SP"] = new()
        {
            [Servico.Autorizacao] = ("https://nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx", "https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx"),
            [Servico.RecepcaoEvento] = ("https://nfe.fazenda.sp.gov.br/ws/nferecepcaoevento4.asmx", "https://homologacao.nfe.fazenda.sp.gov.br/ws/nferecepcaoevento4.asmx"),
            [Servico.Inutilizacao] = ("https://nfe.fazenda.sp.gov.br/ws/nfeinutilizacao4.asmx", "https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeinutilizacao4.asmx"),
            [Servico.ConsultaProtocolo] = ("https://nfe.fazenda.sp.gov.br/ws/nfeconsultaprotocolo4.asmx", "https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeconsultaprotocolo4.asmx"),
        },
    };

    // SVRS (SEFAZ Virtual RS) — provedor padrão usado pelos estados sem webservice próprio e
    // sem entrada em _porUf. Também é o fallback de ObterUrl para qualquer UF desconhecida.
    private static readonly Dictionary<Servico, (string Producao, string Homologacao)> _svrs = new()
    {
        [Servico.Autorizacao] = ("https://nfe.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx", "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx"),
        [Servico.RecepcaoEvento] = ("https://nfe.svrs.rs.gov.br/ws/recepcaoevento/recepcaoevento4.asmx", "https://nfe-homologacao.svrs.rs.gov.br/ws/recepcaoevento/recepcaoevento4.asmx"),
        [Servico.Inutilizacao] = ("https://nfe.svrs.rs.gov.br/ws/nfeinutilizacao/nfeinutilizacao4.asmx", "https://nfe-homologacao.svrs.rs.gov.br/ws/nfeinutilizacao/nfeinutilizacao4.asmx"),
        [Servico.ConsultaProtocolo] = ("https://nfe.svrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx", "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx"),
    };

    // SVAN (SEFAZ Virtual do Ambiente Nacional, domínio sefazvirtual.fazenda.gov.br). Autorizador
    // primário do MA; contingência (SVC-AN) para as demais UFs que não usam SVC-RS.
    // ATENÇÃO: o domínio antigo svc.fazenda.gov.br está fora do ar (NXDOMAIN verificado
    // 2026-08-18) — foi substituído por sefazvirtual.fazenda.gov.br na unificação SVAN/SVC-AN.
    private static readonly Dictionary<Servico, (string Producao, string Homologacao)> _svan = new()
    {
        [Servico.Autorizacao] = ("https://www.sefazvirtual.fazenda.gov.br/NFeAutorizacao4/NFeAutorizacao4.asmx", "https://hom.sefazvirtual.fazenda.gov.br/NFeAutorizacao4/NFeAutorizacao4.asmx"),
        [Servico.RecepcaoEvento] = ("https://www.sefazvirtual.fazenda.gov.br/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx", "https://hom.sefazvirtual.fazenda.gov.br/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx"),
        [Servico.Inutilizacao] = ("https://www.sefazvirtual.fazenda.gov.br/NFeInutilizacao4/NFeInutilizacao4.asmx", "https://hom.sefazvirtual.fazenda.gov.br/NFeInutilizacao4/NFeInutilizacao4.asmx"),
        [Servico.ConsultaProtocolo] = ("https://www.sefazvirtual.fazenda.gov.br/NFeConsultaProtocolo4/NFeConsultaProtocolo4.asmx", "https://hom.sefazvirtual.fazenda.gov.br/NFeConsultaProtocolo4/NFeConsultaProtocolo4.asmx"),
    };

    // Estados que usam SVC-RS como contingência (confirmado em sefaz.rs.gov.br/nfe/nfe-svc.aspx
    // em 2026-08-18). Todas as demais UFs usam SVC-AN. O código anterior tinha essa lista errada
    // (continha CE/PA/PI/RN/RS, que na verdade usam SVC-AN, e não tinha PR).
    private static readonly HashSet<string> _estadosSvcRs = ["AM", "BA", "GO", "MA", "MS", "MT", "PE", "PR"];

    public SefazService(ILogger<SefazService> logger, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        // Por padrão homologação usa stub (CLAUDE.md: "ISefazService: stub em dev/homologação,
        // real em produção"). Permite override via Sefaz:UseRealWebservice=true para quem tiver
        // credenciamento real de homologação na SEFAZ.
        _useRealWebservice = config.GetValue<bool>("Sefaz:UseRealWebservice", false);
    }

    private bool UsaStub(Empresa empresa) => empresa.AmbienteSefaz == AmbienteSefaz.Homologacao && !_useRealWebservice;

    public async Task<SefazResultado> EnviarNFeAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default)
    {
        if (empresa.CertificadoBytes == null)
            return new SefazResultado(false, null, null, null, "Certificado não configurado.", 0);

        if (UsaStub(empresa))
            return SimularAutorizacao(nota);

        var resultado = await TentarEnvioAsync(nota, empresa, ObterUrl(empresa.Uf, empresa.AmbienteSefaz, Servico.Autorizacao), ct);

        if (!resultado.Sucesso && IsErroConexao(resultado))
        {
            _logger.LogWarning("SEFAZ primária indisponível para nota {Numero}. Tentando contingência.", nota.Numero);
            var urlContingencia = ObterUrlContingencia(empresa.Uf, empresa.AmbienteSefaz, Servico.Autorizacao);
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
            using var client = CriarHttpClientComCertificado(empresa);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

            var soapEnvelope = MontarSoapEnvioNFe(nota.XmlEnvio!);
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

    public async Task<SefazResultado> CancelarNFeAsync(NotaFiscal nota, Empresa empresa, string xmlEventoAssinado, CancellationToken ct = default)
    {
        if (UsaStub(empresa))
            return SimularEvento(nota.ChaveAcesso!, "Cancelamento");

        return await EnviarEventoAsync(xmlEventoAssinado, empresa, ct, contexto: $"cancelamento da nota {nota.Numero}");
    }

    public async Task<SefazConsultaResultado> ConsultarChaveAcessoAsync(string chaveAcesso, Empresa empresa, CancellationToken ct = default)
    {
        if (UsaStub(empresa))
            return new SefazConsultaResultado(true, "Autorizada", "HOM-STUB", DateTime.UtcNow, null);

        try
        {
            using var client = CriarHttpClientComCertificado(empresa, timeoutSeconds: 15);
            var url = ObterUrl(empresa.Uf, empresa.AmbienteSefaz, Servico.ConsultaProtocolo);

            var consulta =
                $"""
                 <consSitNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
                   <tpAmb>{(int)empresa.AmbienteSefaz}</tpAmb>
                   <xServ>CONSULTAR</xServ>
                   <chNFe>{chaveAcesso}</chNFe>
                 </consSitNFe>
                 """;
            var envelope = MontarSoapGenerico(consulta, "http://www.portalfiscal.inf.br/nfe/wsdl/NFeConsultaProtocolo4");
            var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeConsultaProtocolo4/nfeConsultaNF");

            var response = await client.PostAsync(url, content, ct);
            var xml = await response.Content.ReadAsStringAsync(ct);

            return ParsearConsultaProtocolo(xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar chave de acesso {Chave} na SEFAZ", chaveAcesso);
            return new SefazConsultaResultado(false, null, null, null, null);
        }
    }

    private SefazConsultaResultado ParsearConsultaProtocolo(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

            var protNode = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt", ns);
            if (protNode == null)
            {
                var cStatLote = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText;
                var xMotivoLote = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText;
                return new SefazConsultaResultado(false, xMotivoLote ?? "Nota não encontrada", null, null, xml);
            }

            var cStat = protNode.SelectSingleNode("nfe:cStat", ns)?.InnerText;
            var xMotivo = protNode.SelectSingleNode("nfe:xMotivo", ns)?.InnerText ?? "";
            var nProt = protNode.SelectSingleNode("nfe:nProt", ns)?.InnerText;
            var dhRecbtoStr = protNode.SelectSingleNode("nfe:dhRecbto", ns)?.InnerText;
            DateTime? dhRecbto = DateTimeOffset.TryParse(dhRecbtoStr, out var dto) ? dto.UtcDateTime : null;

            var situacao = cStat switch
            {
                "100" => "Autorizada",
                "101" or "151" => "Cancelada",
                "110" or "301" or "302" => "Denegada",
                _ => xMotivo
            };

            return new SefazConsultaResultado(true, situacao, nProt, dhRecbto, xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao interpretar retorno de consulta de protocolo");
            return new SefazConsultaResultado(false, null, null, null, xml);
        }
    }

    public async Task<bool> ConsultarStatusServicoAsync(Empresa empresa, CancellationToken ct = default)
    {
        try
        {
            if (empresa.CertificadoBytes == null) return false;

            using var client = CriarHttpClientComCertificado(empresa, timeoutSeconds: 10);
            var url = ObterUrl(empresa.Uf, empresa.AmbienteSefaz, Servico.Autorizacao);
            var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ---- Eventos fiscais (CC-e, Manifestação) ----------------------------------------------
    // Todos usam RecepcaoEvento — mesmo mecanismo do cancelamento, apenas com tpEvento/detEvento
    // diferentes já embutidos no XML assinado recebido em evento.XmlEvento.

    public async Task<SefazResultado> EnviarEventoCceAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default)
    {
        if (UsaStub(empresa))
            return SimularEvento(evento.ChaveAcesso!, "CC-e");

        return await EnviarEventoAsync(evento.XmlEvento!, empresa, ct, contexto: $"CC-e sequencial {evento.SequencialCce}");
    }

    public async Task<SefazResultado> EnviarManifestacaoAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default)
    {
        if (UsaStub(empresa))
            return SimularEvento(evento.ChaveAcesso!, "Manifestação");

        return await EnviarEventoAsync(evento.XmlEvento!, empresa, ct, contexto: $"manifestação {evento.Tipo}");
    }

    private async Task<SefazResultado> EnviarEventoAsync(string xmlEventoAssinado, Empresa empresa, CancellationToken ct, string contexto)
    {
        try
        {
            using var client = CriarHttpClientComCertificado(empresa);
            var url = ObterUrl(empresa.Uf, empresa.AmbienteSefaz, Servico.RecepcaoEvento);

            var envelope = MontarSoapGenerico(xmlEventoAssinado, "http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4");
            var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4/nfeRecepcaoEvento");

            var response = await client.PostAsync(url, content, ct);
            var xml = await response.Content.ReadAsStringAsync(ct);

            return ParsearRetornoEvento(xml);
        }
        catch (TaskCanceledException)
        {
            return new SefazResultado(false, null, null, null, "Timeout na comunicação com SEFAZ.", -2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao transmitir evento à SEFAZ ({Contexto})", contexto);
            return new SefazResultado(false, null, null, null, "Erro na comunicação com a SEFAZ.", 0);
        }
    }

    private SefazResultado ParsearRetornoEvento(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

            var infEvento = doc.SelectSingleNode("//nfe:retEvento/nfe:infEvento", ns);
            if (infEvento == null)
            {
                var cStatLote = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText ?? "0";
                var xMotivoLote = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText ?? "";
                var amostra = xml.Length > 200 ? xml[..200] + "..." : xml;
                return new SefazResultado(false, null, null, xml,
                    $"[{cStatLote}] {xMotivoLote} — retorno sem <infEvento>. Início: \"{amostra.Trim()}\"",
                    int.TryParse(cStatLote, out var cl) ? cl : 0);
            }

            var cStat = infEvento.SelectSingleNode("nfe:cStat", ns)?.InnerText ?? "0";
            var xMotivo = infEvento.SelectSingleNode("nfe:xMotivo", ns)?.InnerText ?? "";
            var chNFe = infEvento.SelectSingleNode("nfe:chNFe", ns)?.InnerText;
            var nProt = infEvento.SelectSingleNode("nfe:nProt", ns)?.InnerText;

            var codigo = int.TryParse(cStat, out var c) ? c : 0;
            // 135 = Evento registrado e vinculado a NF-e (código de sucesso para cancelamento,
            // CC-e e manifestação do destinatário).
            var sucesso = codigo == 135;

            return new SefazResultado(sucesso, chNFe, nProt, xml, sucesso ? null : $"[{cStat}] {xMotivo}", codigo);
        }
        catch (XmlException ex)
        {
            var amostra = xml.Length > 200 ? xml[..200] + "..." : xml;
            _logger.LogError(ex, "Retorno SEFAZ (evento) não é XML válido. Amostra: {Amostra}", amostra);
            return new SefazResultado(false, null, null, null,
                $"Resposta da SEFAZ não é XML válido. Início: \"{amostra.Trim()}\"", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar retorno de evento SEFAZ");
            return new SefazResultado(false, null, null, null,
                $"Erro inesperado ao interpretar retorno da SEFAZ: {ex.Message}", 0);
        }
    }

    public async Task<SefazResultado> EnviarInutilizacaoAsync(EventoFiscal evento, Empresa empresa, CancellationToken ct = default)
    {
        if (UsaStub(empresa))
        {
            var protocolo = $"INU{DateTime.UtcNow:yyMMddHHmmss}";
            _logger.LogInformation(
                "[HOM-STUB] Inutilização simulada — empresa {Cnpj} série {Serie} {Ini}-{Fin}",
                empresa.Cnpj, evento.SerieInutilizacao, evento.NumeroInicialInutilizacao, evento.NumeroFinalInutilizacao);
            return new SefazResultado(true, null, protocolo, null, null, 102);
        }

        try
        {
            using var client = CriarHttpClientComCertificado(empresa);
            var url = ObterUrl(empresa.Uf, empresa.AmbienteSefaz, Servico.Inutilizacao);

            var envelope = MontarSoapGenerico(evento.XmlEvento!, "http://www.portalfiscal.inf.br/nfe/wsdl/NFeInutilizacao4");
            var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://www.portalfiscal.inf.br/nfe/wsdl/NFeInutilizacao4/nfeInutilizacaoNF");

            var response = await client.PostAsync(url, content, ct);
            var xml = await response.Content.ReadAsStringAsync(ct);

            return ParsearRetornoInutilizacao(xml);
        }
        catch (TaskCanceledException)
        {
            return new SefazResultado(false, null, null, null, "Timeout na comunicação com SEFAZ.", -2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao transmitir inutilização à SEFAZ");
            return new SefazResultado(false, null, null, null, "Erro na comunicação com a SEFAZ.", 0);
        }
    }

    private SefazResultado ParsearRetornoInutilizacao(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

            var infInut = doc.SelectSingleNode("//nfe:retInutNFe/nfe:infInut", ns);
            if (infInut == null)
            {
                var amostra = xml.Length > 200 ? xml[..200] + "..." : xml;
                return new SefazResultado(false, null, null, xml,
                    $"Retorno da SEFAZ não contém <infInut>. Início: \"{amostra.Trim()}\"", 0);
            }

            var cStat = infInut.SelectSingleNode("nfe:cStat", ns)?.InnerText ?? "0";
            var xMotivo = infInut.SelectSingleNode("nfe:xMotivo", ns)?.InnerText ?? "";
            var nProt = infInut.SelectSingleNode("nfe:nProt", ns)?.InnerText;

            var codigo = int.TryParse(cStat, out var c) ? c : 0;
            var sucesso = codigo == 102; // 102 = Inutilização de número homologado

            return new SefazResultado(sucesso, null, nProt, xml, sucesso ? null : $"[{cStat}] {xMotivo}", codigo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao interpretar retorno de inutilização");
            return new SefazResultado(false, null, null, xml, $"Erro ao interpretar retorno: {ex.Message}", 0);
        }
    }

    // ---- Helpers compartilhados -------------------------------------------------------------

    private static HttpClient CriarHttpClientComCertificado(Empresa empresa, int timeoutSeconds = 30)
    {
        var cert = new X509Certificate2(empresa.CertificadoBytes!, empresa.CertificadoSenha,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        // Validação do certificado do SERVIDOR usa a cadeia de confiança padrão do SO — os
        // webservices da SEFAZ têm certificados TLS públicos válidos. Nunca desabilitar essa
        // validação (havia um DangerousAcceptAnyServerCertificateValidator aqui antes — abria a
        // porta para MITM na única chamada que realmente sai para a internet com o certificado
        // A1 do cliente).

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    private static string MontarSoapGenerico(string corpoXml, string wsdlNamespace) =>
        $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                 xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
                 xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <nfeDadosMsg xmlns=""{wsdlNamespace}"">
      {corpoXml}
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";

    private string MontarSoapEnvioNFe(string xmlNFe)
    {
        var corpo =
            $"""
             <enviNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
               <idLote>{DateTime.UtcNow:yyyyMMddHHmmssfff}</idLote>
               <indSinc>1</indSinc>
               {xmlNFe}
             </enviNFe>
             """;
        return MontarSoapGenerico(corpo, "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4");
    }

    private string ObterUrl(string uf, AmbienteSefaz ambiente, Servico servico)
    {
        var ufUpper = uf.ToUpperInvariant();

        // MA não tem webservice próprio nem usa SVRS como autorizador padrão — usa SVAN.
        if (ufUpper == "MA")
            return ambiente == AmbienteSefaz.Producao ? _svan[servico].Producao : _svan[servico].Homologacao;

        if (_porUf.TryGetValue(ufUpper, out var servicos) && servicos.TryGetValue(servico, out var par))
            return ambiente == AmbienteSefaz.Producao ? par.Producao : par.Homologacao;

        return ambiente == AmbienteSefaz.Producao ? _svrs[servico].Producao : _svrs[servico].Homologacao;
    }

    private string ObterUrlContingencia(string uf, AmbienteSefaz ambiente, Servico servico)
    {
        var par = _estadosSvcRs.Contains(uf.ToUpperInvariant()) ? _svrs[servico] : _svan[servico];
        return ambiente == AmbienteSefaz.Producao ? par.Producao : par.Homologacao;
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

            if (doc.SelectSingleNode("//nfe:cStat", ns) == null)
            {
                var amostra = xml.Length > 200 ? xml[..200] + "..." : xml;
                _logger.LogWarning("Retorno SEFAZ sem cStat. Amostra: {Amostra}", amostra);
                return new SefazResultado(false, null, null, xml,
                    $"Retorno da SEFAZ não contém cStat. Início: \"{amostra.Trim()}\"", 0);
            }

            var codigo = int.TryParse(cStat, out var c) ? c : 0;
            var sucesso = codigo == 100; // 100 = Autorizada

            return new SefazResultado(sucesso, chNFe, nProt, xml,
                sucesso ? null : $"[{cStat}] {xMotivo}", codigo);
        }
        catch (XmlException ex)
        {
            var amostra = xml.Length > 200 ? xml[..200] + "..." : xml;
            _logger.LogError(ex, "Retorno SEFAZ não é XML válido. Amostra: {Amostra}", amostra);
            return new SefazResultado(false, null, null, null,
                $"Resposta da SEFAZ não é XML válido. Início: \"{amostra.Trim()}\"", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar retorno SEFAZ");
            return new SefazResultado(false, null, null, null,
                $"Erro inesperado ao interpretar retorno da SEFAZ: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Stub de autorização para ambiente de homologação. Extrai a chave de acesso do XML
    /// gerado pelo XmlNFeService (Id="NFe{chave}") e devolve sucesso com protocolo sintético.
    /// </summary>
    private SefazResultado SimularAutorizacao(NotaFiscal nota)
    {
        var chave = ExtrairChaveAcesso(nota.XmlEnvio) ?? new string('0', 44);
        var protocolo = $"HOM{DateTime.UtcNow:yyMMddHHmmssfff}";
        var xmlRetorno =
            $"""
             <?xml version="1.0" encoding="UTF-8"?>
             <retEnviNFe xmlns="http://www.portalfiscal.inf.br/nfe" versao="4.00">
               <tpAmb>2</tpAmb>
               <verAplic>HOM_STUB</verAplic>
               <cStat>104</cStat>
               <xMotivo>Lote processado (simulado em homologação)</xMotivo>
               <protNFe versao="4.00">
                 <infProt>
                   <tpAmb>2</tpAmb>
                   <verAplic>HOM_STUB</verAplic>
                   <chNFe>{chave}</chNFe>
                   <dhRecbto>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:sszzz}</dhRecbto>
                   <nProt>{protocolo}</nProt>
                   <digVal>STUB</digVal>
                   <cStat>100</cStat>
                   <xMotivo>Autorizado o uso da NF-e</xMotivo>
                 </infProt>
               </protNFe>
             </retEnviNFe>
             """;
        _logger.LogInformation(
            "[HOM-STUB] NF-e {Numero} simulada como autorizada. Chave: {Chave} Protocolo: {Protocolo}",
            nota.Numero, chave, protocolo);
        return new SefazResultado(true, chave, protocolo, xmlRetorno, null, 100);
    }

    /// <summary>Stub genérico para eventos (cancelamento, CC-e, manifestação) em homologação.</summary>
    private SefazResultado SimularEvento(string chaveAcesso, string tipoLabel)
    {
        var protocolo = $"HOM{DateTime.UtcNow:yyMMddHHmmssfff}";
        _logger.LogInformation(
            "[HOM-STUB] {Tipo} simulado como aceito. Chave: {Chave} Protocolo: {Protocolo}",
            tipoLabel, chaveAcesso, protocolo);
        return new SefazResultado(true, chaveAcesso, protocolo, null, null, 135);
    }

    private static string? ExtrairChaveAcesso(string? xmlEnvio)
    {
        if (string.IsNullOrWhiteSpace(xmlEnvio)) return null;
        // O XmlNFeService grava Id="NFe{chave de 44 dígitos}" no elemento infNFe.
        var match = Regex.Match(xmlEnvio, @"Id=""NFe(\d{44})""");
        return match.Success ? match.Groups[1].Value : null;
    }
}
