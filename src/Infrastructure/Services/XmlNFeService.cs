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
        sb.AppendLine($"<cNF>{new Random().Next(10000000, 99999999)}</cNF>");
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
        sb.AppendLine($"<xNome>{empresa.RazaoSocial}</xNome>");
        sb.AppendLine($"<xFant>{empresa.NomeFantasia}</xFant>");
        sb.AppendLine("<enderEmit>");
        sb.AppendLine($"<xLgr>{empresa.Logradouro}</xLgr>");
        sb.AppendLine($"<nro>{empresa.Numero}</nro>");
        sb.AppendLine($"<xBairro>{empresa.Bairro}</xBairro>");
        sb.AppendLine($"<cMun>{empresa.CodigoMunicipio}</cMun>");
        sb.AppendLine($"<xMun>{empresa.Cidade}</xMun>");
        sb.AppendLine($"<UF>{empresa.Uf}</UF>");
        sb.AppendLine($"<CEP>{empresa.Cep}</CEP>");
        sb.AppendLine("<cPais>1058</cPais>");
        sb.AppendLine("<xPais>Brasil</xPais>");
        sb.AppendLine($"<fone>{empresa.Telefone}</fone>");
        sb.AppendLine("</enderEmit>");
        sb.AppendLine($"<IE>{empresa.InscricaoEstadual}</IE>");
        sb.AppendLine($"<CRT>{(int)empresa.RegimeTributario}</CRT>");
        sb.AppendLine("</emit>");

        // DESTINATÁRIO
        if (!string.IsNullOrEmpty(nota.DestinatarioCpfCnpj))
        {
            sb.AppendLine("<dest>");
            var tagDoc = nota.DestinatarioTipoPessoa == TipoPessoa.PessoaFisica ? "CPF" : "CNPJ";
            sb.AppendLine($"<{tagDoc}>{nota.DestinatarioCpfCnpj}</{tagDoc}>");
            sb.AppendLine($"<xNome>{nota.DestinatarioRazaoSocial}</xNome>");
            if (!string.IsNullOrEmpty(nota.DestinatarioLogradouro))
            {
                sb.AppendLine("<enderDest>");
                sb.AppendLine($"<xLgr>{nota.DestinatarioLogradouro}</xLgr>");
                sb.AppendLine($"<nro>{nota.DestinatarioNumero}</nro>");
                sb.AppendLine($"<xBairro>{nota.DestinatarioBairro}</xBairro>");
                sb.AppendLine($"<cMun>{nota.DestinatarioCodigoMunicipio}</cMun>");
                sb.AppendLine($"<xMun>{nota.DestinatarioCidade}</xMun>");
                sb.AppendLine($"<UF>{nota.DestinatarioUf}</UF>");
                sb.AppendLine($"<CEP>{nota.DestinatarioCep}</CEP>");
                sb.AppendLine("<cPais>1058</cPais>");
                sb.AppendLine("<xPais>Brasil</xPais>");
                sb.AppendLine("</enderDest>");
            }
            sb.AppendLine($"<indIEDest>{(string.IsNullOrEmpty(nota.DestinatarioInscricaoEstadual) ? 9 : 1)}</indIEDest>");
            if (!string.IsNullOrEmpty(nota.DestinatarioInscricaoEstadual))
                sb.AppendLine($"<IE>{nota.DestinatarioInscricaoEstadual}</IE>");
            if (!string.IsNullOrEmpty(nota.DestinatarioEmail))
                sb.AppendLine($"<email>{nota.DestinatarioEmail}</email>");
            sb.AppendLine("</dest>");
        }

        // ITENS
        foreach (var item in nota.Itens)
        {
            sb.AppendLine($"<det nItem=\"{item.NumeroItem}\">");
            sb.AppendLine("<prod>");
            sb.AppendLine($"<cProd>{item.CodigoProduto}</cProd>");
            sb.AppendLine($"<cEAN>{item.CodigoEan ?? "SEM GTIN"}</cEAN>");
            sb.AppendLine($"<xProd>{item.Descricao}</xProd>");
            sb.AppendLine($"<NCM>{item.Ncm}</NCM>");
            if (!string.IsNullOrEmpty(item.Cest)) sb.AppendLine($"<CEST>{item.Cest}</CEST>");
            sb.AppendLine($"<CFOP>{item.Cfop}</CFOP>");
            sb.AppendLine($"<uCom>{item.UnidadeComercial}</uCom>");
            sb.AppendLine($"<qCom>{item.Quantidade:F4}</qCom>");
            sb.AppendLine($"<vUnCom>{item.ValorUnitario:F4}</vUnCom>");
            sb.AppendLine($"<vProd>{item.ValorTotal:F2}</vProd>");
            sb.AppendLine($"<cEANTrib>{item.CodigoEan ?? "SEM GTIN"}</cEANTrib>");
            sb.AppendLine($"<uTrib>{item.UnidadeComercial}</uTrib>");
            sb.AppendLine($"<qTrib>{item.Quantidade:F4}</qTrib>");
            sb.AppendLine($"<vUnTrib>{item.ValorUnitario:F4}</vUnTrib>");
            sb.AppendLine($"<indTot>1</indTot>");
            if (item.ValorDesconto > 0) sb.AppendLine($"<vDesc>{item.ValorDesconto:F2}</vDesc>");
            sb.AppendLine("</prod>");

            // IMPOSTOS
            sb.AppendLine("<imposto>");
            sb.AppendLine($"<vTotTrib>{(item.ValorIcms + item.ValorPis + item.ValorCofins):F2}</vTotTrib>");

            // ICMS
            sb.AppendLine("<ICMS>");
            var cstIcmsStr = ((int)item.CstIcms).ToString("D2");
            sb.AppendLine($"<ICMS{cstIcmsStr}>");
            sb.AppendLine($"<orig>{(int)item.OrigemMercadoria}</orig>");
            sb.AppendLine($"<CST>{cstIcmsStr}</CST>");
            if (item.BaseCalculoIcms > 0)
            {
                sb.AppendLine("<modBC>3</modBC>");
                sb.AppendLine($"<vBC>{item.BaseCalculoIcms:F2}</vBC>");
                sb.AppendLine($"<pICMS>{item.AliquotaIcms:F2}</pICMS>");
                sb.AppendLine($"<vICMS>{item.ValorIcms:F2}</vICMS>");
            }
            sb.AppendLine($"</ICMS{cstIcmsStr}>");
            sb.AppendLine("</ICMS>");

            // PIS
            sb.AppendLine("<PIS>");
            var cstPisStr = ((int)item.CstPis).ToString("D2");
            sb.AppendLine($"<PISAliq>");
            sb.AppendLine($"<CST>{cstPisStr}</CST>");
            sb.AppendLine($"<vBC>{item.BaseCalculoPis:F2}</vBC>");
            sb.AppendLine($"<pPIS>{item.AliquotaPis:F2}</pPIS>");
            sb.AppendLine($"<vPIS>{item.ValorPis:F2}</vPIS>");
            sb.AppendLine("</PISAliq>");
            sb.AppendLine("</PIS>");

            // COFINS
            sb.AppendLine("<COFINS>");
            var cstCofinsStr = ((int)item.CstCofins).ToString("D2");
            sb.AppendLine("<COFINSAliq>");
            sb.AppendLine($"<CST>{cstCofinsStr}</CST>");
            sb.AppendLine($"<vBC>{item.BaseCalculoCofins:F2}</vBC>");
            sb.AppendLine($"<pCOFINS>{item.AliquotaCofins:F2}</pCOFINS>");
            sb.AppendLine($"<vCOFINS>{item.ValorCofins:F2}</vCOFINS>");
            sb.AppendLine("</COFINSAliq>");
            sb.AppendLine("</COFINS>");

            sb.AppendLine("</imposto>");
            sb.AppendLine("</det>");
        }

        // TOTAIS
        sb.AppendLine("<total>");
        sb.AppendLine("<ICMSTot>");
        sb.AppendLine($"<vBC>{nota.Itens.Sum(i => i.BaseCalculoIcms):F2}</vBC>");
        sb.AppendLine($"<vICMS>{nota.TotalIcms:F2}</vICMS>");
        sb.AppendLine($"<vICMSDeson>0.00</vICMSDeson>");
        sb.AppendLine($"<vFCP>0.00</vFCP>");
        sb.AppendLine($"<vBCST>{nota.Itens.Sum(i => i.BaseCalculoIcmsSt ?? 0):F2}</vBCST>");
        sb.AppendLine($"<vST>{nota.TotalIcmsSt:F2}</vST>");
        sb.AppendLine($"<vFCPST>0.00</vFCPST>");
        sb.AppendLine($"<vFCPSTRet>0.00</vFCPSTRet>");
        sb.AppendLine($"<vProd>{nota.TotalProdutos:F2}</vProd>");
        sb.AppendLine($"<vFrete>{nota.TotalFrete:F2}</vFrete>");
        sb.AppendLine($"<vSeg>{nota.TotalSeguro:F2}</vSeg>");
        sb.AppendLine($"<vDesc>{nota.TotalDesconto:F2}</vDesc>");
        sb.AppendLine($"<vII>0.00</vII>");
        sb.AppendLine($"<vIPI>0.00</vIPI>");
        sb.AppendLine($"<vIPIDevol>0.00</vIPIDevol>");
        sb.AppendLine($"<vPIS>{nota.TotalPis:F2}</vPIS>");
        sb.AppendLine($"<vCOFINS>{nota.TotalCofins:F2}</vCOFINS>");
        sb.AppendLine($"<vOutro>0.00</vOutro>");
        sb.AppendLine($"<vNF>{nota.TotalNota:F2}</vNF>");
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
        sb.AppendLine($"<vPag>{nota.ValorPagamento:F2}</vPag>");
        sb.AppendLine("</detPag>");
        sb.AppendLine("</pag>");

        if (!string.IsNullOrEmpty(nota.InformacoesAdicionais))
        {
            sb.AppendLine("<infAdic>");
            sb.AppendLine($"<infCpl>{nota.InformacoesAdicionais}</infCpl>");
            sb.AppendLine("</infAdic>");
        }

        sb.AppendLine("</infNFe>");
        sb.AppendLine("</NFe>");
        sb.AppendLine("</nfeProc>");

        return sb.ToString();
    }

    public string AssinarXml(string xml, byte[] certificadoBytes, string senha)
    {
        try
        {
            var cert = new X509Certificate2(certificadoBytes, senha,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(xml);

            var nfe = doc.GetElementsByTagName("NFe")[0] as XmlElement;
            var infNFe = doc.GetElementsByTagName("infNFe")[0] as XmlElement;

            if (nfe == null || infNFe == null) return xml;

            var signedXml = new SignedXml(nfe);
            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;
            signedXml.SigningKey = cert.GetRSAPrivateKey();

            var reference = new Reference("#" + infNFe.GetAttribute("Id"));
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigC14NTransform());
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();

            var signature = signedXml.GetXml();
            nfe.AppendChild(doc.ImportNode(signature, true));

            return doc.OuterXml;
        }
        catch
        {
            // In production, log and rethrow
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
    <xJust>{justificativa}</xJust>
  </infCanc>
</cancNFe>";
    }

    public bool ValidarXml(string xml, out IEnumerable<string> erros)
    {
        var listaErros = new List<string>();
        // In production: validate against XSD schemas from SEFAZ
        // For now, basic XML parse check
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            erros = listaErros;
            return true;
        }
        catch (Exception ex)
        {
            _ = ex;
            listaErros.Add("O XML da nota fiscal não é válido. Verifique a estrutura do documento.");
            erros = listaErros;
            return false;
        }
    }

    private static string GerarChaveAcesso(int cuf, DateTime emissao, string cnpj,
        int modelo, int serie, int numero, int tpEmis, int ambiente)
    {
        var chave = $"{cuf}{emissao:yyMM}{cnpj}{modelo:D2}{serie:D3}{numero:D9}{tpEmis}{new Random().Next(10000000, 99999999):D8}";
        var dv = CalcularDv(chave);
        return chave + dv;
    }

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
