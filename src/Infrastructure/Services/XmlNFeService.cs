using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Infrastructure.Services;

public class XmlNFeService : IXmlNFeService
{
    private readonly IXsdValidationService _xsd;

    public XmlNFeService(IXsdValidationService xsd) => _xsd = xsd;

    // Escape de texto livre interpolado no XML — protege contra quebra de tag e XML Injection.
    // Aplicar em qualquer valor controlado por usuário (razão social, descrições, informações adicionais).
    private static string E(string? s) =>
        string.IsNullOrEmpty(s) ? "" : System.Security.SecurityElement.Escape(s)!;

    // Formatação decimal forçando InvariantCulture — SEFAZ exige ponto como separador decimal.
    // Sem isso, interpolação `{x:F2}` usa a cultura corrente e em pt-BR gera vírgula → rejeição pelo schema.
    private static string F2(decimal v) => v.ToString("F2", CultureInfo.InvariantCulture);
    private static string F4(decimal v) => v.ToString("F4", CultureInfo.InvariantCulture);

    public string GerarXmlNFe(NotaFiscal nota, Empresa empresa)
    {
        var sb = new StringBuilder();
        var cuf = ObterCUf(empresa.Uf);
        var chave = GerarChaveAcesso(cuf, nota.DataEmissao, empresa.Cnpj, (int)nota.Tipo,
            nota.Serie, nota.Numero, 1, (int)nota.Ambiente);

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\">");
        sb.AppendLine($"<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\">");
        sb.AppendLine("<infNFe versao=\"4.00\" Id=\"NFe" + chave + "\">");

        // IDE
        sb.AppendLine("<ide>");
        sb.AppendLine($"<cUF>{cuf}</cUF>");
        sb.AppendLine($"<cNF>{CodigoNumericoAleatorio()}</cNF>");
        sb.AppendLine($"<natOp>VENDA DE MERCADORIA</natOp>");
        sb.AppendLine($"<mod>{(int)nota.Tipo}</mod>");
        sb.AppendLine($"<serie>{nota.Serie:D3}</serie>");
        sb.AppendLine($"<nNF>{nota.Numero:D9}</nNF>");
        sb.AppendLine($"<dhEmi>{nota.DataEmissao:yyyy-MM-ddTHH:mm:sszzz}</dhEmi>");
        sb.AppendLine($"<tpNF>{(int)nota.TipoOperacao}</tpNF>");
        sb.AppendLine($"<idDest>1</idDest>");
        sb.AppendLine($"<cMunFG>{empresa.CodigoMunicipio}</cMunFG>");
        sb.AppendLine($"<tpImp>1</tpImp>");
        sb.AppendLine($"<tpEmis>1</tpEmis>");
        sb.AppendLine($"<cDV>0</cDV>");
        sb.AppendLine($"<tpAmb>{(int)nota.Ambiente}</tpAmb>");
        sb.AppendLine($"<finNFe>{(int)nota.Finalidade}</finNFe>");
        sb.AppendLine($"<indFinal>1</indFinal>");
        sb.AppendLine($"<indPres>1</indPres>");
        sb.AppendLine($"<procEmi>0</procEmi>");
        sb.AppendLine($"<verProc>NfeSaas 1.0</verProc>");
        sb.AppendLine("</ide>");

        // EMITENTE
        sb.AppendLine("<emit>");
        sb.AppendLine($"<CNPJ>{empresa.Cnpj}</CNPJ>");
        sb.AppendLine($"<xNome>{E(empresa.RazaoSocial)}</xNome>");
        sb.AppendLine($"<xFant>{E(empresa.NomeFantasia)}</xFant>");
        sb.AppendLine("<enderEmit>");
        sb.AppendLine($"<xLgr>{E(empresa.Logradouro)}</xLgr>");
        sb.AppendLine($"<nro>{E(empresa.Numero)}</nro>");
        sb.AppendLine($"<xBairro>{E(empresa.Bairro)}</xBairro>");
        sb.AppendLine($"<cMun>{empresa.CodigoMunicipio}</cMun>");
        sb.AppendLine($"<xMun>{E(empresa.Cidade)}</xMun>");
        sb.AppendLine($"<UF>{empresa.Uf}</UF>");
        sb.AppendLine($"<CEP>{empresa.Cep}</CEP>");
        sb.AppendLine("<cPais>1058</cPais>");
        sb.AppendLine("<xPais>Brasil</xPais>");
        sb.AppendLine($"<fone>{E(empresa.Telefone)}</fone>");
        sb.AppendLine("</enderEmit>");
        sb.AppendLine($"<IE>{E(empresa.InscricaoEstadual)}</IE>");
        sb.AppendLine($"<CRT>{(int)empresa.RegimeTributario}</CRT>");
        sb.AppendLine("</emit>");

        // DESTINATÁRIO
        if (!string.IsNullOrEmpty(nota.DestinatarioCpfCnpj))
        {
            sb.AppendLine("<dest>");
            var tagDoc = nota.DestinatarioTipoPessoa == TipoPessoa.PessoaFisica ? "CPF" : "CNPJ";
            sb.AppendLine($"<{tagDoc}>{nota.DestinatarioCpfCnpj}</{tagDoc}>");
            sb.AppendLine($"<xNome>{E(nota.DestinatarioRazaoSocial)}</xNome>");
            if (!string.IsNullOrEmpty(nota.DestinatarioLogradouro))
            {
                sb.AppendLine("<enderDest>");
                sb.AppendLine($"<xLgr>{E(nota.DestinatarioLogradouro)}</xLgr>");
                sb.AppendLine($"<nro>{E(nota.DestinatarioNumero)}</nro>");
                sb.AppendLine($"<xBairro>{E(nota.DestinatarioBairro)}</xBairro>");
                sb.AppendLine($"<cMun>{nota.DestinatarioCodigoMunicipio}</cMun>");
                sb.AppendLine($"<xMun>{E(nota.DestinatarioCidade)}</xMun>");
                sb.AppendLine($"<UF>{nota.DestinatarioUf}</UF>");
                sb.AppendLine($"<CEP>{nota.DestinatarioCep}</CEP>");
                sb.AppendLine("<cPais>1058</cPais>");
                sb.AppendLine("<xPais>Brasil</xPais>");
                sb.AppendLine("</enderDest>");
            }
            sb.AppendLine($"<indIEDest>{(string.IsNullOrEmpty(nota.DestinatarioInscricaoEstadual) ? 9 : 1)}</indIEDest>");
            if (!string.IsNullOrEmpty(nota.DestinatarioInscricaoEstadual))
                sb.AppendLine($"<IE>{E(nota.DestinatarioInscricaoEstadual)}</IE>");
            if (!string.IsNullOrEmpty(nota.DestinatarioEmail))
                sb.AppendLine($"<email>{E(nota.DestinatarioEmail)}</email>");
            sb.AppendLine("</dest>");
        }

        // ITENS
        foreach (var item in nota.Itens)
        {
            sb.AppendLine($"<det nItem=\"{item.NumeroItem}\">");
            sb.AppendLine("<prod>");
            sb.AppendLine($"<cProd>{E(item.CodigoProduto)}</cProd>");
            sb.AppendLine($"<cEAN>{E(item.CodigoEan ?? "SEM GTIN")}</cEAN>");
            sb.AppendLine($"<xProd>{E(item.Descricao)}</xProd>");
            sb.AppendLine($"<NCM>{item.Ncm}</NCM>");
            if (!string.IsNullOrEmpty(item.Cest)) sb.AppendLine($"<CEST>{item.Cest}</CEST>");
            sb.AppendLine($"<CFOP>{item.Cfop}</CFOP>");
            sb.AppendLine($"<uCom>{E(item.UnidadeComercial)}</uCom>");
            sb.AppendLine($"<qCom>{F4(item.Quantidade)}</qCom>");
            sb.AppendLine($"<vUnCom>{F4(item.ValorUnitario)}</vUnCom>");
            sb.AppendLine($"<vProd>{F2(item.ValorTotal)}</vProd>");
            sb.AppendLine($"<cEANTrib>{E(item.CodigoEan ?? "SEM GTIN")}</cEANTrib>");
            sb.AppendLine($"<uTrib>{E(item.UnidadeComercial)}</uTrib>");
            sb.AppendLine($"<qTrib>{F4(item.Quantidade)}</qTrib>");
            sb.AppendLine($"<vUnTrib>{F4(item.ValorUnitario)}</vUnTrib>");
            sb.AppendLine($"<indTot>1</indTot>");
            if (item.ValorDesconto > 0) sb.AppendLine($"<vDesc>{F2(item.ValorDesconto)}</vDesc>");
            sb.AppendLine("</prod>");

            // IMPOSTOS
            sb.AppendLine("<imposto>");
            sb.AppendLine($"<vTotTrib>{F2((item.ValorIcms + item.ValorPis + item.ValorCofins))}</vTotTrib>");

            // ICMS — Simples Nacional usa CSOSN dentro de <ICMSSN{xx}>; Regime Normal usa CST dentro de <ICMS{xx}>.
            sb.AppendLine("<ICMS>");
            if (item.CsosnIcms.HasValue)
            {
                var csosnStr = ((int)item.CsosnIcms.Value).ToString();
                sb.AppendLine($"<ICMSSN{csosnStr}>");
                sb.AppendLine($"<orig>{(int)item.OrigemMercadoria}</orig>");
                sb.AppendLine($"<CSOSN>{csosnStr}</CSOSN>");
                switch (item.CsosnIcms.Value)
                {
                    case NfeSaas.Domain.Enums.CsosnIcms.TributadaComPermissaoCredito: // 101
                        sb.AppendLine($"<pCredSN>{F4(item.AliquotaIcms)}</pCredSN>");
                        sb.AppendLine($"<vCredICMSSN>{F2((item.BaseCalculoIcms * item.AliquotaIcms / 100m))}</vCredICMSSN>");
                        break;
                    case NfeSaas.Domain.Enums.CsosnIcms.Outros: // 900
                        if (item.BaseCalculoIcms > 0)
                        {
                            sb.AppendLine("<modBC>3</modBC>");
                            sb.AppendLine($"<vBC>{F2(item.BaseCalculoIcms)}</vBC>");
                            sb.AppendLine($"<pICMS>{F2(item.AliquotaIcms)}</pICMS>");
                            sb.AppendLine($"<vICMS>{F2(item.ValorIcms)}</vICMS>");
                        }
                        break;
                    // 102, 103, 300, 400, 500, 201, 202, 203 — bloco mínimo (orig + CSOSN). ST detalhada será tratada na tributação avançada.
                }
                sb.AppendLine($"</ICMSSN{csosnStr}>");
            }
            else
            {
                var cstIcmsStr = ((int)item.CstIcms).ToString("D2");
                sb.AppendLine($"<ICMS{cstIcmsStr}>");
                sb.AppendLine($"<orig>{(int)item.OrigemMercadoria}</orig>");
                sb.AppendLine($"<CST>{cstIcmsStr}</CST>");
                if (item.BaseCalculoIcms > 0)
                {
                    sb.AppendLine("<modBC>3</modBC>");
                    sb.AppendLine($"<vBC>{F2(item.BaseCalculoIcms)}</vBC>");
                    sb.AppendLine($"<pICMS>{F2(item.AliquotaIcms)}</pICMS>");
                    sb.AppendLine($"<vICMS>{F2(item.ValorIcms)}</vICMS>");
                }
                // FCP — % adicional sobre BC ICMS, em algumas UFs (CE 2%, RJ 2% etc.)
                if (item.ValorFcp.HasValue && item.ValorFcp.Value > 0)
                {
                    sb.AppendLine($"<vBCFCP>{F2(item.BaseCalculoFcp ?? 0)}</vBCFCP>");
                    sb.AppendLine($"<pFCP>{F2(item.AliquotaFcp ?? 0)}</pFCP>");
                    sb.AppendLine($"<vFCP>{F2(item.ValorFcp.Value)}</vFCP>");
                }
                sb.AppendLine($"</ICMS{cstIcmsStr}>");
            }
            sb.AppendLine("</ICMS>");

            // PIS
            sb.AppendLine("<PIS>");
            var cstPisStr = ((int)item.CstPis).ToString("D2");
            sb.AppendLine($"<PISAliq>");
            sb.AppendLine($"<CST>{cstPisStr}</CST>");
            sb.AppendLine($"<vBC>{F2(item.BaseCalculoPis)}</vBC>");
            sb.AppendLine($"<pPIS>{F2(item.AliquotaPis)}</pPIS>");
            sb.AppendLine($"<vPIS>{F2(item.ValorPis)}</vPIS>");
            sb.AppendLine("</PISAliq>");
            sb.AppendLine("</PIS>");

            // COFINS
            sb.AppendLine("<COFINS>");
            var cstCofinsStr = ((int)item.CstCofins).ToString("D2");
            sb.AppendLine("<COFINSAliq>");
            sb.AppendLine($"<CST>{cstCofinsStr}</CST>");
            sb.AppendLine($"<vBC>{F2(item.BaseCalculoCofins)}</vBC>");
            sb.AppendLine($"<pCOFINS>{F2(item.AliquotaCofins)}</pCOFINS>");
            sb.AppendLine($"<vCOFINS>{F2(item.ValorCofins)}</vCOFINS>");
            sb.AppendLine("</COFINSAliq>");
            sb.AppendLine("</COFINS>");

            // IPI (se calculado)
            if (item.ValorIpi.HasValue && item.ValorIpi.Value > 0)
            {
                sb.AppendLine("<IPI>");
                sb.AppendLine("<cEnq>999</cEnq>"); // 999 = Outros (default seguro)
                var cstIpiStr = item.CstIpi ?? "50";
                sb.AppendLine($"<IPITrib>");
                sb.AppendLine($"<CST>{cstIpiStr}</CST>");
                sb.AppendLine($"<vBC>{F2(item.BaseCalculoIpi ?? 0)}</vBC>");
                sb.AppendLine($"<pIPI>{F2(item.AliquotaIpi ?? 0)}</pIPI>");
                sb.AppendLine($"<vIPI>{F2(item.ValorIpi.Value)}</vIPI>");
                sb.AppendLine("</IPITrib>");
                sb.AppendLine("</IPI>");
            }

            // ICMSUFDest — DIFAL (operação interestadual a consumidor final não-contribuinte)
            if (item.ValorIcmsUfDestino.HasValue && item.ValorIcmsUfDestino.Value > 0)
            {
                sb.AppendLine("<ICMSUFDest>");
                sb.AppendLine($"<vBCUFDest>{F2(item.BaseCalculoDifal ?? 0)}</vBCUFDest>");
                sb.AppendLine($"<pFCPUFDest>0.00</pFCPUFDest>");
                sb.AppendLine($"<pICMSUFDest>{F2(item.AliquotaInternaUfDestino ?? 0)}</pICMSUFDest>");
                sb.AppendLine($"<pICMSInter>{F2(item.AliquotaInterestadual ?? 0)}</pICMSInter>");
                sb.AppendLine($"<pICMSInterPart>100.00</pICMSInterPart>"); // Partilha 100% destino desde 2019
                sb.AppendLine($"<vFCPUFDest>0.00</vFCPUFDest>");
                sb.AppendLine($"<vICMSUFDest>{F2(item.ValorIcmsUfDestino.Value)}</vICMSUFDest>");
                sb.AppendLine($"<vICMSUFRemet>{F2(item.ValorIcmsUfRemetente ?? 0)}</vICMSUFRemet>");
                sb.AppendLine("</ICMSUFDest>");
            }

            sb.AppendLine("</imposto>");
            sb.AppendLine("</det>");
        }

        // TOTAIS
        sb.AppendLine("<total>");
        sb.AppendLine("<ICMSTot>");
        sb.AppendLine($"<vBC>{F2(nota.Itens.Sum(i => i.BaseCalculoIcms))}</vBC>");
        sb.AppendLine($"<vICMS>{F2(nota.TotalIcms)}</vICMS>");
        sb.AppendLine($"<vICMSDeson>0.00</vICMSDeson>");
        sb.AppendLine($"<vFCPUFDest>0.00</vFCPUFDest>");
        sb.AppendLine($"<vICMSUFDest>{F2(nota.TotalIcmsUfDestino)}</vICMSUFDest>");
        sb.AppendLine($"<vICMSUFRemet>{F2(nota.TotalIcmsUfRemetente)}</vICMSUFRemet>");
        sb.AppendLine($"<vFCP>{F2(nota.TotalFcp)}</vFCP>");
        sb.AppendLine($"<vBCST>{F2(nota.Itens.Sum(i => i.BaseCalculoIcmsSt ?? 0))}</vBCST>");
        sb.AppendLine($"<vST>{F2(nota.TotalIcmsSt)}</vST>");
        sb.AppendLine($"<vFCPST>0.00</vFCPST>");
        sb.AppendLine($"<vFCPSTRet>0.00</vFCPSTRet>");
        sb.AppendLine($"<vProd>{F2(nota.TotalProdutos)}</vProd>");
        sb.AppendLine($"<vFrete>{F2(nota.TotalFrete)}</vFrete>");
        sb.AppendLine($"<vSeg>{F2(nota.TotalSeguro)}</vSeg>");
        sb.AppendLine($"<vDesc>{F2(nota.TotalDesconto)}</vDesc>");
        sb.AppendLine($"<vII>0.00</vII>");
        sb.AppendLine($"<vIPI>{F2(nota.TotalIpi)}</vIPI>");
        sb.AppendLine($"<vIPIDevol>0.00</vIPIDevol>");
        sb.AppendLine($"<vPIS>{F2(nota.TotalPis)}</vPIS>");
        sb.AppendLine($"<vCOFINS>{F2(nota.TotalCofins)}</vCOFINS>");
        sb.AppendLine($"<vOutro>0.00</vOutro>");
        sb.AppendLine($"<vNF>{F2(nota.TotalNota)}</vNF>");
        sb.AppendLine("</ICMSTot>");
        sb.AppendLine("</total>");

        // TRANSPORTE
        sb.AppendLine("<transp>");
        sb.AppendLine($"<modFrete>{(int)nota.ModalidadeFrete}</modFrete>");
        sb.AppendLine("</transp>");

        // PAGAMENTO
        sb.AppendLine("<pag>");
        sb.AppendLine("<detPag>");
        sb.AppendLine($"<tPag>{nota.FormaPagemento}</tPag>");
        sb.AppendLine($"<vPag>{F2(nota.ValorPagamento)}</vPag>");
        sb.AppendLine("</detPag>");
        sb.AppendLine("</pag>");

        if (!string.IsNullOrEmpty(nota.InformacoesAdicionais))
        {
            sb.AppendLine("<infAdic>");
            sb.AppendLine($"<infCpl>{E(nota.InformacoesAdicionais)}</infCpl>");
            sb.AppendLine("</infAdic>");
        }

        sb.AppendLine("</infNFe>");
        sb.AppendLine("</NFe>");
        sb.AppendLine("</nfeProc>");

        return sb.ToString();
    }

    public string AssinarXml(string xml, byte[] certificadoBytes, string senha) =>
        AssinarPorTags(xml, parentTag: "NFe", childTagComId: "infNFe", certificadoBytes, senha);

    public string AssinarEvento(string xml, byte[] certificadoBytes, string senha) =>
        AssinarPorTags(xml, parentTag: "evento", childTagComId: "infEvento", certificadoBytes, senha);

    public string AssinarInutilizacao(string xml, byte[] certificadoBytes, string senha) =>
        AssinarPorTags(xml, parentTag: "inutNFe", childTagComId: "infInut", certificadoBytes, senha);

    public string AssinarCancelamento(string xml, byte[] certificadoBytes, string senha) =>
        AssinarPorTags(xml, parentTag: "cancNFe", childTagComId: "infCanc", certificadoBytes, senha);

    private static string AssinarPorTags(string xml, string parentTag, string childTagComId, byte[] certificadoBytes, string senha)
    {
        try
        {
            var cert = new X509Certificate2(certificadoBytes, senha,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(xml);

            var parent = doc.GetElementsByTagName(parentTag)[0] as XmlElement;
            var child = doc.GetElementsByTagName(childTagComId)[0] as XmlElement;
            if (parent == null || child == null) return xml;

            var signedXml = new SignedXml(parent);
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;
            signedXml.SigningKey = cert.GetRSAPrivateKey();

            var reference = new Reference("#" + child.GetAttribute("Id"));
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();

            var signature = signedXml.GetXml();
            parent.AppendChild(doc.ImportNode(signature, true));

            return doc.OuterXml;
        }
        catch
        {
            // Em produção: registrar log e propagar. Falha silenciosa só é aceitável em desenvolvimento.
            return xml;
        }
    }

    public string GerarXmlCancelamento(string chaveAcesso, string justificativa, Empresa empresa)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<cancNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
  <infCanc Id=""ID{chaveAcesso}"">
    <tpAmb>{(int)empresa.AmbienteSefaz}</tpAmb>
    <xServ>CANCELAR</xServ>
    <chNFe>{chaveAcesso}</chNFe>
    <nProt></nProt>
    <xJust>{E(justificativa)}</xJust>
  </infCanc>
</cancNFe>";
    }

    public string GerarXmlCce(string chaveAcesso, int sequencial, string correcao, Empresa empresa)
    {
        var cuf = ObterCUf(empresa.Uf);
        var cOrgao = cuf.ToString("D2");
        var idEvento = $"ID110110{chaveAcesso}{sequencial:D2}";
        var dh = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<envEvento xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""1.00"">
  <idLote>{DateTime.UtcNow:yyyyMMddHHmmssfff}</idLote>
  <evento versao=""1.00"">
    <infEvento Id=""{idEvento}"">
      <cOrgao>{cOrgao}</cOrgao>
      <tpAmb>{(int)empresa.AmbienteSefaz}</tpAmb>
      <CNPJ>{empresa.Cnpj}</CNPJ>
      <chNFe>{chaveAcesso}</chNFe>
      <dhEvento>{dh}</dhEvento>
      <tpEvento>110110</tpEvento>
      <nSeqEvento>{sequencial}</nSeqEvento>
      <verEvento>1.00</verEvento>
      <detEvento versao=""1.00"">
        <descEvento>Carta de Correcao</descEvento>
        <xCorrecao>{System.Security.SecurityElement.Escape(correcao)}</xCorrecao>
        <xCondUso>A Carta de Correcao e disciplinada pelo paragrafo 1o-A do art. 7o do Convenio S/N, de 15 de dezembro de 1970 e pode ser utilizada para regularizacao de erro ocorrido na emissao de documento fiscal, desde que o erro nao esteja relacionado com: I - as variaveis que determinam o valor do imposto tais como: base de calculo, aliquota, diferenca de preco, quantidade, valor da operacao ou da prestacao; II - a correcao de dados cadastrais que implique mudanca do remetente ou do destinatario; III - a data de emissao ou de saida.</xCondUso>
      </detEvento>
    </infEvento>
  </evento>
</envEvento>";
    }

    public string GerarXmlInutilizacao(Empresa empresa, int ano, NfeSaas.Domain.Enums.TipoNota tipo, int serie, int numIni, int numFin, string justificativa)
    {
        var cuf = ObterCUf(empresa.Uf);
        var anoStr = (ano % 100).ToString("D2");
        // ID inutilização: "ID" + cUF + ano(2) + CNPJ + mod + serie + nNFIni + nNFFin
        var idInut = $"ID{cuf:D2}{anoStr}{empresa.Cnpj}{(int)tipo:D2}{serie:D3}{numIni:D9}{numFin:D9}";

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<inutNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
  <infInut Id=""{idInut}"">
    <tpAmb>{(int)empresa.AmbienteSefaz}</tpAmb>
    <xServ>INUTILIZAR</xServ>
    <cUF>{cuf:D2}</cUF>
    <ano>{anoStr}</ano>
    <CNPJ>{empresa.Cnpj}</CNPJ>
    <mod>{(int)tipo}</mod>
    <serie>{serie}</serie>
    <nNFIni>{numIni}</nNFIni>
    <nNFFin>{numFin}</nNFFin>
    <xJust>{System.Security.SecurityElement.Escape(justificativa)}</xJust>
  </infInut>
</inutNFe>";
    }

    public string GerarXmlManifestacao(string chaveAcesso, NfeSaas.Domain.Enums.TipoEventoFiscal tipo, string justificativa, Empresa empresa)
    {
        var tpEvento = ((int)tipo).ToString();
        var nSeq = 1;
        var idEvento = $"ID{tpEvento}{chaveAcesso}{nSeq:D2}";
        var dh = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");
        var descEvento = tipo switch
        {
            NfeSaas.Domain.Enums.TipoEventoFiscal.ManifestacaoConfirmacao => "Confirmacao da Operacao",
            NfeSaas.Domain.Enums.TipoEventoFiscal.ManifestacaoCiencia => "Ciencia da Operacao",
            NfeSaas.Domain.Enums.TipoEventoFiscal.ManifestacaoDesconhecimento => "Desconhecimento da Operacao",
            NfeSaas.Domain.Enums.TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada => "Operacao nao Realizada",
            _ => "Manifestacao do Destinatario"
        };

        // Operação Não Realizada exige <xJust>; demais não.
        var justTag = tipo == NfeSaas.Domain.Enums.TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada
            ? $"<xJust>{System.Security.SecurityElement.Escape(justificativa)}</xJust>"
            : "";

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<envEvento xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""1.00"">
  <idLote>{DateTime.UtcNow:yyyyMMddHHmmssfff}</idLote>
  <evento versao=""1.00"">
    <infEvento Id=""{idEvento}"">
      <cOrgao>91</cOrgao>
      <tpAmb>{(int)empresa.AmbienteSefaz}</tpAmb>
      <CNPJ>{empresa.Cnpj}</CNPJ>
      <chNFe>{chaveAcesso}</chNFe>
      <dhEvento>{dh}</dhEvento>
      <tpEvento>{tpEvento}</tpEvento>
      <nSeqEvento>{nSeq}</nSeqEvento>
      <verEvento>1.00</verEvento>
      <detEvento versao=""1.00"">
        <descEvento>{descEvento}</descEvento>
        {justTag}
      </detEvento>
    </infEvento>
  </evento>
</envEvento>";
    }

    public bool ValidarXml(string xml, out IEnumerable<string> erros)
    {
        var listaErros = new List<string>();

        // 1. Validação XSD (oficial SEFAZ ou skeleton — depende do que está bundleado em /Schemas/).
        var xsdResultado = _xsd.Validar(xml);
        if (!xsdResultado.Pulada)
        {
            listaErros.AddRange(xsdResultado.Erros);
            // Se XSD falhou, não vale a pena seguir com checagem estrutural — XML já é inválido.
            if (!xsdResultado.Valido)
            {
                erros = listaErros;
                return false;
            }
        }

        // 2. Validação estrutural ("schema-lite") — complementa XSD com checagens de negócio:
        // chave 44 dígitos, CNPJ emitente válido, presença de assinatura digital, etc.
        XmlDocument doc;
        try
        {
            doc = new XmlDocument();
            doc.LoadXml(xml);
        }
        catch
        {
            listaErros.Add("O XML da nota fiscal não está bem-formado.");
            erros = listaErros;
            return false;
        }

        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        var infNFe = doc.SelectSingleNode("//nfe:infNFe", ns);
        if (infNFe == null)
        {
            listaErros.Add("XML não contém o elemento obrigatório <infNFe>.");
            erros = listaErros;
            return false;
        }

        var idAttr = infNFe.Attributes?["Id"]?.Value;
        if (string.IsNullOrEmpty(idAttr) || !idAttr.StartsWith("NFe") || idAttr.Length != 47)
            listaErros.Add("Atributo Id de <infNFe> deve estar no formato 'NFe' + 44 dígitos.");
        else
        {
            var chave = idAttr[3..];
            if (!chave.All(char.IsDigit))
                listaErros.Add("Chave de acesso deve conter apenas dígitos.");
        }

        // Tags estruturais obrigatórias
        string[] obrigatorias = ["nfe:ide", "nfe:emit", "nfe:dest", "nfe:det", "nfe:total", "nfe:transp"];
        foreach (var tag in obrigatorias)
        {
            if (infNFe.SelectSingleNode(tag, ns) == null)
                listaErros.Add($"Tag obrigatória ausente: <{tag.Replace("nfe:", "")}>.");
        }

        // CNPJ do emitente deve ser válido
        var cnpjEmit = infNFe.SelectSingleNode("nfe:emit/nfe:CNPJ", ns)?.InnerText;
        if (string.IsNullOrEmpty(cnpjEmit) || !Domain.Services.CnpjValidator.Validar(cnpjEmit))
            listaErros.Add("CNPJ do emitente ausente ou inválido no XML.");

        // Pelo menos um item
        var itens = infNFe.SelectNodes("nfe:det", ns);
        if (itens == null || itens.Count == 0)
            listaErros.Add("XML deve conter ao menos um item <det>.");

        // Assinatura presente (NFe assinada)
        var sig = doc.SelectSingleNode("//*[local-name()='Signature']");
        if (sig == null)
            listaErros.Add("Assinatura digital ausente no XML.");

        erros = listaErros;
        return listaErros.Count == 0;
    }

    private static string GerarChaveAcesso(int cuf, DateTime emissao, string cnpj,
        int modelo, int serie, int numero, int tpEmis, int ambiente)
    {
        var chave = $"{cuf}{emissao:yyMM}{cnpj}{modelo:D2}{serie:D3}{numero:D9}{tpEmis}{CodigoNumericoAleatorio():D8}";
        var dv = CalcularDv(chave);
        return chave + dv;
    }

    // cNF (8 dígitos): código numérico anti-enumeração da chave de acesso. Exige RNG criptográfico —
    // System.Random é determinístico pelo tick do relógio e permite previsão da chave.
    private static int CodigoNumericoAleatorio() =>
        RandomNumberGenerator.GetInt32(10_000_000, 100_000_000);

    private static int CalcularDv(string chave)
    {
        var peso = 2;
        var soma = 0;
        for (var i = chave.Length - 1; i >= 0; i--)
        {
            soma += int.Parse(chave[i].ToString()) * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static int ObterCUf(string uf) => uf.ToUpper() switch
    {
        "AC" => 12, "AL" => 27, "AP" => 16, "AM" => 13, "BA" => 29,
        "CE" => 23, "DF" => 53, "ES" => 32, "GO" => 52, "MA" => 21,
        "MT" => 51, "MS" => 50, "MG" => 31, "PA" => 15, "PB" => 25,
        "PR" => 41, "PE" => 26, "PI" => 22, "RJ" => 33, "RN" => 24,
        "RS" => 43, "RO" => 11, "RR" => 14, "SC" => 42, "SP" => 35,
        "SE" => 28, "TO" => 17, _ => 35
    };
}
