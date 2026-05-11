using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NfeSaas.Infrastructure.Services;

public class DanfeService : IDanfeService
{
    public async Task<byte[]> GerarDanfePdfAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return await Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader(nota, empresa));
                    page.Content().Element(ComposeContent(nota, empresa));
                    page.Footer().Element(ComposeFooter(nota));
                });
            });

            return document.GeneratePdf();
        }, ct);
    }

    public async Task<byte[]> GerarDanfeNFCePdfAsync(NotaFiscal nota, Empresa empresa, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return await Task.Run(() =>
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(80, 500, Unit.Millimetre); // Thermal 80mm
                    page.Margin(3, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));

                    page.Content().Element(ComposeNFCe(nota, empresa));
                });
            });

            return document.GeneratePdf();
        }, ct);
    }

    private Action<IContainer> ComposeHeader(NotaFiscal nota, Empresa empresa) => container =>
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Column(c =>
                {
                    c.Item().Text("DANFE").Bold().FontSize(14);
                    c.Item().Text("Documento Auxiliar da Nota Fiscal Eletrônica").FontSize(7);
                });
                row.RelativeItem(5).Column(c =>
                {
                    c.Item().Border(1).Padding(4).Column(info =>
                    {
                        info.Item().Text($"Nº {nota.Numero:D9}").Bold();
                        info.Item().Text($"Série: {nota.Serie:D3}");
                        info.Item().Text($"Emissão: {nota.DataEmissao:dd/MM/yyyy HH:mm}");
                        info.Item().Text($"Situação: {nota.Situacao}").Bold();
                    });
                });
                row.RelativeItem(2).Column(c =>
                {
                    c.Item().Border(1).Padding(4).Column(amb =>
                    {
                        if (nota.Ambiente == NfeSaas.Domain.Enums.AmbienteSefaz.Homologacao)
                            amb.Item().Background("#FFD700").Text("HOMOLOGAÇÃO").Bold().FontSize(10);
                        else
                            amb.Item().Text("PRODUÇÃO").Bold();
                    });
                });
            });

            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Border(1).Padding(4).Column(emit =>
                {
                    emit.Item().Text("EMITENTE").Bold();
                    emit.Item().Text(empresa.RazaoSocial).Bold();
                    emit.Item().Text($"CNPJ: {FormatarCnpj(empresa.Cnpj)}");
                    emit.Item().Text($"{empresa.Logradouro}, {empresa.Numero} - {empresa.Bairro}");
                    emit.Item().Text($"{empresa.Cidade}/{empresa.Uf} - CEP: {empresa.Cep}");
                    emit.Item().Text($"IE: {empresa.InscricaoEstadual}");
                });
            });

            if (!string.IsNullOrEmpty(nota.ChaveAcesso))
            {
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Column(chave =>
                    {
                        chave.Item().Text("Chave de Acesso:").Bold().FontSize(7);
                        chave.Item().Text(FormatarChaveAcesso(nota.ChaveAcesso!)).FontSize(8).FontFamily("Courier New");
                        // Code128 da chave (44 dígitos) — formato padrão para impressão de DANFE
                        chave.Item().PaddingTop(2).Height(35)
                            .Image(QrCodeService.GerarCode128Png(nota.ChaveAcesso!, 600, 60));
                        chave.Item().PaddingTop(2).Text("Consulte em www.nfe.fazenda.gov.br/portal").FontSize(6);
                    });
                    row.ConstantItem(80).AlignRight().Image(QrCodeService.GerarQrCodePng(QrCodeService.MontarUrlConsultaNFe(nota), 6));
                });
            }
        });
    };

    private Action<IContainer> ComposeContent(NotaFiscal nota, Empresa empresa) => container =>
    {
        container.Column(col =>
        {
            // Destinatário
            col.Item().PaddingTop(5).Border(1).Padding(4).Column(dest =>
            {
                dest.Item().Text("DESTINATÁRIO").Bold();
                dest.Item().Text($"Nome: {nota.DestinatarioRazaoSocial}");
                if (!string.IsNullOrEmpty(nota.DestinatarioCpfCnpj))
                    dest.Item().Text($"CPF/CNPJ: {nota.DestinatarioCpfCnpj}");
                if (!string.IsNullOrEmpty(nota.DestinatarioLogradouro))
                    dest.Item().Text($"End: {nota.DestinatarioLogradouro}, {nota.DestinatarioNumero} - {nota.DestinatarioCidade}/{nota.DestinatarioUf}");
            });

            // Itens
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(30);
                    cols.RelativeColumn(3);
                    cols.ConstantColumn(30);
                    cols.ConstantColumn(20);
                    cols.ConstantColumn(40);
                    cols.ConstantColumn(40);
                    cols.ConstantColumn(50);
                    cols.ConstantColumn(40);
                    cols.ConstantColumn(45);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#CCCCCC").Text("#").Bold();
                    header.Cell().Background("#CCCCCC").Text("Descrição").Bold();
                    header.Cell().Background("#CCCCCC").Text("NCM").Bold();
                    header.Cell().Background("#CCCCCC").Text("UN").Bold();
                    header.Cell().Background("#CCCCCC").Text("Qtd").Bold();
                    header.Cell().Background("#CCCCCC").Text("V.Unit").Bold();
                    header.Cell().Background("#CCCCCC").Text("Desconto").Bold();
                    header.Cell().Background("#CCCCCC").Text("ICMS%").Bold();
                    header.Cell().Background("#CCCCCC").Text("V.Total").Bold();
                });

                foreach (var item in nota.Itens)
                {
                    table.Cell().Text(item.NumeroItem.ToString());
                    table.Cell().Text(item.Descricao);
                    table.Cell().Text(item.Ncm);
                    table.Cell().Text(item.UnidadeComercial);
                    table.Cell().AlignRight().Text(item.Quantidade.ToString("F2"));
                    table.Cell().AlignRight().Text(item.ValorUnitario.ToString("F2"));
                    table.Cell().AlignRight().Text(item.ValorDesconto.ToString("F2"));
                    table.Cell().AlignRight().Text(item.AliquotaIcms.ToString("F2") + "%");
                    table.Cell().AlignRight().Text(item.ValorTotal.ToString("F2"));
                }
            });

            // Totais
            col.Item().PaddingTop(5).AlignRight().Column(tot =>
            {
                tot.Item().Text($"Total Produtos: R$ {nota.TotalProdutos:F2}").Bold();
                if (nota.TotalDesconto > 0) tot.Item().Text($"Desconto: R$ {nota.TotalDesconto:F2}");
                if (nota.TotalFrete > 0) tot.Item().Text($"Frete: R$ {nota.TotalFrete:F2}");
                tot.Item().Text($"Total ICMS: R$ {nota.TotalIcms:F2}");
                tot.Item().Text($"Total PIS: R$ {nota.TotalPis:F2}");
                tot.Item().Text($"Total COFINS: R$ {nota.TotalCofins:F2}");
                tot.Item().Text($"VALOR TOTAL DA NOTA: R$ {nota.TotalNota:F2}").Bold().FontSize(11);
            });

            if (!string.IsNullOrEmpty(nota.InformacoesAdicionais))
            {
                col.Item().PaddingTop(5).Border(1).Padding(4).Column(inf =>
                {
                    inf.Item().Text("INFORMAÇÕES ADICIONAIS:").Bold();
                    inf.Item().Text(nota.InformacoesAdicionais);
                });
            }

            if (!string.IsNullOrEmpty(nota.Protocolo))
            {
                col.Item().PaddingTop(3).Text($"Protocolo de Autorização: {nota.Protocolo}").Bold();
                if (nota.DataAutorizacao.HasValue)
                    col.Item().Text($"Data/Hora Autorização: {nota.DataAutorizacao:dd/MM/yyyy HH:mm:ss}");
            }
        });
    };

    private Action<IContainer> ComposeFooter(NotaFiscal nota) => container =>
    {
        container.AlignCenter().Text($"NF-e gerada pelo NfeSaas • Ambiente: {nota.Ambiente}").FontSize(7);
    };

    private Action<IContainer> ComposeNFCe(NotaFiscal nota, Empresa empresa) => container =>
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(empresa.RazaoSocial).Bold().FontSize(9);
            col.Item().AlignCenter().Text($"CNPJ: {FormatarCnpj(empresa.Cnpj)}").FontSize(7);
            col.Item().AlignCenter().Text($"{empresa.Logradouro}, {empresa.Numero}").FontSize(7);
            col.Item().AlignCenter().Text("NFC-e - Nota Fiscal de Consumidor Eletrônica").Bold();
            col.Item().PaddingTop(3).LineHorizontal(0.5f);

            foreach (var item in nota.Itens)
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text(item.Descricao).FontSize(7);
                    row.ConstantItem(60).AlignRight().Text($"R$ {item.ValorTotal:F2}").FontSize(7);
                });
                col.Item().Text($"  {item.Quantidade:F2} {item.UnidadeComercial} x R$ {item.ValorUnitario:F4}").FontSize(6);
            }

            col.Item().LineHorizontal(0.5f);
            col.Item().AlignRight().Text($"TOTAL: R$ {nota.TotalNota:F2}").Bold().FontSize(10);

            if (!string.IsNullOrEmpty(nota.ChaveAcesso))
            {
                // QR Code obrigatório pra NFC-e (Manual de Padrões Técnicos NFC-e)
                col.Item().PaddingTop(5).AlignCenter().Text("Consulte pela Chave de Acesso em").FontSize(7).Bold();
                col.Item().AlignCenter().Text(EnderecoConsultaUf(empresa)).FontSize(6);
                col.Item().AlignCenter().Text(FormatarChaveAcesso(nota.ChaveAcesso!)).FontSize(7).FontFamily("Courier New");

                col.Item().PaddingTop(3).AlignCenter()
                    .Width(200).Height(200)
                    .Image(QrCodeService.GerarQrCodePng(QrCodeService.MontarUrlConsultaNFCe(nota, empresa), 6));

                if (string.IsNullOrEmpty(empresa.CscToken))
                    col.Item().AlignCenter().Text("(QR sem hash CSC — configure em /empresas)").FontSize(5).Italic();
            }

            if (!string.IsNullOrEmpty(nota.Protocolo))
                col.Item().PaddingTop(3).AlignCenter().Text($"Protocolo: {nota.Protocolo}").FontSize(7);
        });
    };

    private static string EnderecoConsultaUf(Empresa empresa) => empresa.Uf.ToUpper() switch
    {
        "SP" => "www.nfce.fazenda.sp.gov.br",
        "RJ" => "www.fazenda.rj.gov.br",
        "MG" => "nfce.fazenda.mg.gov.br",
        "PR" => "www.fazenda.pr.gov.br/nfce",
        "RS" => "www.sefaz.rs.gov.br/NFCE",
        _    => "www.svrs.rs.gov.br/nfce/consulta"
    };

    private static string FormatarCnpj(string cnpj)
    {
        if (cnpj.Length != 14) return cnpj;
        return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}";
    }

    private static string FormatarChaveAcesso(string chave)
    {
        if (chave.Length != 44) return chave;
        return string.Join(" ", Enumerable.Range(0, 11).Select(i => chave.Substring(i * 4, 4)));
    }
}
