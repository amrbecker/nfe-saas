using NfeSaas.Domain.Enums;

namespace NfeSaas.Application.DTOs;

public record EmitirNotaFiscalDto(
    TipoNota Tipo,
    FinalidadeNota Finalidade,
    TipoOperacao TipoOperacao,
    DestinatarioDto Destinatario,
    List<ItemNotaDto> Itens,
    TransporteDto Transporte,
    PagamentoDto Pagamento,
    string? InformacoesAdicionais
);

public record DestinatarioDto(
    string? CpfCnpj,
    string? RazaoSocial,
    string? Email,
    TipoPessoa TipoPessoa,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? CodigoMunicipio,
    string? InscricaoEstadual
);

public record ItemNotaDto(
    string CodigoProduto,
    string Descricao,
    string Ncm,
    string? Cest,
    string Cfop,
    string Unidade,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    string? CodigoEan,
    ImpostosItemDto Impostos
);

public record ImpostosItemDto(
    OrigemMercadoria OrigemMercadoria,
    CstIcms CstIcms,
    decimal AliquotaIcms,
    decimal? PercentualReducaoIcms,
    bool AplicarSt,
    decimal? MvaIcmsSt,
    decimal? AliquotaInternaIcmsSt,
    CstPisCofins CstPis,
    decimal AliquotaPis,
    CstPisCofins CstCofins,
    decimal AliquotaCofins
);

public record TransporteDto(
    ModalidadeFrete ModalidadeFrete,
    string? TransportadoraCpfCnpj,
    string? TransportadoraRazaoSocial,
    decimal Frete,
    decimal Seguro
);

public record PagamentoDto(
    string FormaPagamento,
    decimal Valor
);

// Response DTOs
public record NotaFiscalResumoDto(
    Guid Id,
    TipoNota Tipo,
    int Serie,
    int Numero,
    string? ChaveAcesso,
    SituacaoNota Situacao,
    string? DestinatarioRazaoSocial,
    decimal TotalNota,
    DateTime DataEmissao,
    DateTime? DataAutorizacao
);

public record NotaFiscalDetalheDto(
    Guid Id,
    TipoNota Tipo,
    int Serie,
    int Numero,
    string? ChaveAcesso,
    string? Protocolo,
    SituacaoNota Situacao,
    AmbienteSefaz Ambiente,
    FinalidadeNota Finalidade,
    TipoOperacao TipoOperacao,
    DestinatarioDto Destinatario,
    List<ItemNotaResumoDto> Itens,
    decimal TotalProdutos,
    decimal TotalDesconto,
    decimal TotalIcms,
    decimal TotalIcmsSt,
    decimal TotalPis,
    decimal TotalCofins,
    decimal TotalFrete,
    decimal TotalNota,
    DateTime DataEmissao,
    DateTime? DataAutorizacao,
    string? MotivoRejeicao,
    string? InformacoesAdicionais
);

public record ItemNotaResumoDto(
    int NumeroItem,
    string CodigoProduto,
    string Descricao,
    string Ncm,
    string Cfop,
    string Unidade,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal ValorDesconto,
    decimal ValorTotal,
    decimal ValorIcms,
    decimal ValorPis,
    decimal ValorCofins
);

public record DashboardDto(
    decimal TotalEmitidoMes,
    int TotalNotasEmitidas,
    int TotalNotasAutorizadas,
    int TotalNotasCanceladas,
    int TotalNotasPendentes,
    List<FaturamentoDiarioDto> FaturamentoDiario
);

public record FaturamentoDiarioDto(DateTime Data, decimal Total, int Quantidade);

public record LoginDto(string Email, string Senha);
public record LoginResultDto(string AccessToken, string RefreshToken, string NomeUsuario, string Email, string Role);
public record RefreshTokenDto(string RefreshToken);

public record UploadCertificadoDto(string Senha);
public record CertificadoStatusDto(bool Valido, string? NomeTitular, string? Cnpj, DateTime? Validade, string? Mensagem);
