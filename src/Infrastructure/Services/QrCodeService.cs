using System.Security.Cryptography;
using System.Text;
using BarcodeStandard;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using QRCoder;
using SkiaSharp;

namespace NfeSaas.Infrastructure.Services;

public static class QrCodeService
{
    // URLs oficiais de consulta NFC-e por UF (parcial — fallback SVRS para os demais).
    // Formato V2 (NFCe versão 4.00): URL?p=chNFe|nVersao|tpAmb|cIdToken|cHashQRCode
    private static readonly Dictionary<(string uf, AmbienteSefaz amb), string> _urlsNFCe = new()
    {
        { ("SP", AmbienteSefaz.Producao),    "https://www.nfce.fazenda.sp.gov.br/qrcode" },
        { ("SP", AmbienteSefaz.Homologacao), "https://www.homologacao.nfce.fazenda.sp.gov.br/qrcode" },
        { ("RJ", AmbienteSefaz.Producao),    "http://www4.fazenda.rj.gov.br/consultaNFCe/QRCode" },
        { ("RJ", AmbienteSefaz.Homologacao), "http://www4.fazenda.rj.gov.br/consultaNFCe/QRCode" },
        { ("MG", AmbienteSefaz.Producao),    "https://nfce.fazenda.mg.gov.br/portalnfce/sistema/qrcode.xhtml" },
        { ("MG", AmbienteSefaz.Homologacao), "https://hnfce.fazenda.mg.gov.br/portalnfce/sistema/qrcode.xhtml" },
        { ("PR", AmbienteSefaz.Producao),    "http://www.fazenda.pr.gov.br/nfce/qrcode" },
        { ("PR", AmbienteSefaz.Homologacao), "http://www.fazenda.pr.gov.br/nfce/qrcode" },
        { ("RS", AmbienteSefaz.Producao),    "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx" },
        { ("RS", AmbienteSefaz.Homologacao), "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx" },
        // SVRS fallback — atende a maioria dos demais estados
        { ("DEFAULT", AmbienteSefaz.Producao),    "https://www.svrs.rs.gov.br/nfce/consulta" },
        { ("DEFAULT", AmbienteSefaz.Homologacao), "https://www.svrs.rs.gov.br/nfce/consulta" },
    };

    public static string MontarUrlConsultaNFCe(NotaFiscal nota, Empresa empresa)
    {
        if (string.IsNullOrEmpty(nota.ChaveAcesso)) return string.Empty;

        var baseUrl = _urlsNFCe.TryGetValue((empresa.Uf.ToUpper(), empresa.AmbienteSefaz), out var u)
            ? u
            : _urlsNFCe[("DEFAULT", empresa.AmbienteSefaz)];

        const string nVersao = "2";
        var tpAmb = ((int)empresa.AmbienteSefaz).ToString();

        if (string.IsNullOrEmpty(empresa.CscId) || string.IsNullOrEmpty(empresa.CscToken))
        {
            // Sem CSC configurado — retorna URL de consulta sem hash (fica visível mas SEFAZ rejeitaria em produção real).
            return $"{baseUrl}?p={nota.ChaveAcesso}|{nVersao}|{tpAmb}";
        }

        // V2: cHashQRCode = SHA-1( chNFe + cIdToken + CSC ).hexLower
        var dadosHash = nota.ChaveAcesso + empresa.CscId + empresa.CscToken;
        var hash = Sha1Hex(dadosHash);
        return $"{baseUrl}?p={nota.ChaveAcesso}|{nVersao}|{tpAmb}|{empresa.CscId}|{hash}";
    }

    public static string MontarUrlConsultaNFe(NotaFiscal nota)
    {
        if (string.IsNullOrEmpty(nota.ChaveAcesso)) return string.Empty;
        return $"https://www.nfe.fazenda.gov.br/portal/consultaRecaptcha.aspx?chNFe={nota.ChaveAcesso}";
    }

    public static byte[] GerarQrCodePng(string conteudo, int pixelsPerModule = 8)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(conteudo, QRCodeGenerator.ECCLevel.M);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Gera barcode Code128 (subset C otimizado para dígitos) em PNG.
    /// Usado no DANFE NFe pra renderizar a chave de acesso de 44 dígitos como código de barras 1D.
    /// </summary>
    public static byte[] GerarCode128Png(string conteudo, int largura = 800, int altura = 80)
    {
        if (string.IsNullOrEmpty(conteudo)) return Array.Empty<byte>();
        var barcode = new Barcode { IncludeLabel = false };
        using var img = barcode.Encode(BarcodeStandard.Type.Code128C, conteudo,
            SKColors.Black, SKColors.White, largura, altura);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static string Sha1Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA1.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
