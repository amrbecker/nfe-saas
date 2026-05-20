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
    decimal AliquotaCofins,
    CsosnIcms? CsosnIcms = null,           // Simples Nacional — quando preenchido, prevalece sobre CstIcms
    decimal? AliquotaIpi = null,           // IPI — % (0-100). null/zero = sem IPI.
    string? CstIpi = null,                 // CST IPI (string 2-dig: 50, 49, 99…). Default "50" quando AliquotaIpi > 0.
    decimal? AliquotaFcp = null,           // FCP — % adicional sobre BC ICMS (0 a ~4 dependendo da UF/produto)
    decimal? AliquotaInternaUfDestino = null  // DIFAL — alíquota interna da UF de destino (usada quando interestadual a não-contribuinte)
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
    string? InformacoesAdicionais,
    DateTime? DataDescarteAutorizado = null,
    bool DentroPeriodoRetencao = false
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
public record LoginResultDto(
    string AccessToken,
    string RefreshToken,
    string NomeUsuario,
    string Email,
    string Role,
    Guid EscritorioId,
    List<EmpresaResumoDto> Empresas,
    AssinaturaDto Assinatura);
public record AssinaturaDto(
    string Plano,
    string Status,                  // TrialAtivo | Pago | TrialExpirado | Suspenso
    int DiasRestantesTrial,         // 0 quando trial terminou ou está em plano pago
    DateTime TrialFimEm,
    DateTime? PlanoAtivoAteEm);
public record LoginFailureDto(string Motivo, string Codigo, AssinaturaDto? Assinatura);
public record RefreshTokenDto(string RefreshToken);
public record SelecionarEmpresaDto(Guid EmpresaId);

public record EmpresaResumoDto(Guid Id, string RazaoSocial, string NomeFantasia, string Cnpj);
public record EscritorioDto(Guid Id, string RazaoSocial, string NomeFantasia, string Cnpj, string Email, string? Telefone, string Plano, bool Ativo);

public record CreateEscritorioDto(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string Email,
    string? Telefone,
    int Plano,                          // 1=Basico, 2=Profissional, 3=Enterprise (Free não existe)
    string NomeAdmin,
    string EmailAdmin,
    string SenhaAdmin
);

public record CadastrarEscritorioComoEmpresaDto(
    string InscricaoEstadual,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    int RegimeTributario,
    int AmbienteSefaz,
    string? Cnae
);

public record AtivarPlanoPagoDto(DateTime AtivoAteUtc, decimal? ValorPago);

public record CreateUsuarioDto(
    string Nome,
    string Email,
    string Senha,
    string Role = "User"
);

public record UpdateUsuarioDto(string Nome, string Role);

public record UsuarioResumoDto(Guid Id, string Nome, string Email, string Role, bool Ativo);

public record CreateEmpresaDto(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string InscricaoEstadual,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    string Telefone,
    string Email,
    int RegimeTributario,
    int AmbienteSefaz,
    string? Cnae = null
);

// === EVENTOS FISCAIS ===
public record EmitirCceDto(string Correcao);
public record ManifestarDto(int Tipo, string? Justificativa);
public record InutilizarDto(int Ano, int TipoNota, int Serie, int NumeroInicial, int NumeroFinal, string Justificativa);

public record EventoFiscalResumoDto(
    Guid Id,
    int Tipo,
    string? ChaveAcesso,
    int? SequencialCce,
    int? AnoInutilizacao,
    int? TipoNotaInutilizacao,
    int? SerieInutilizacao,
    int? NumeroInicialInutilizacao,
    int? NumeroFinalInutilizacao,
    string Justificativa,
    int Situacao,
    string? Protocolo,
    string? MotivoRejeicao,
    DateTime DataEvento,
    DateTime? DataRetorno
);

// === PRODUTO ===
public record CreateProdutoDto(
    string Codigo,
    string Descricao,
    string Ncm,
    string CfopPadrao,
    string UnidadeComercial,
    int OrigemMercadoria,
    decimal ValorUnitarioPadrao,
    string? Cest = null,
    string? CodigoEan = null,
    string? CodigoAnp = null
);

public record UpdateProdutoDto(
    string Codigo,
    string Descricao,
    string Ncm,
    string CfopPadrao,
    string UnidadeComercial,
    int OrigemMercadoria,
    decimal ValorUnitarioPadrao,
    string? Cest,
    string? CodigoEan,
    string? CodigoAnp
);

public record ProdutoResumoDto(
    Guid Id,
    string Codigo,
    string Descricao,
    string Ncm,
    string UnidadeComercial,
    decimal ValorUnitarioPadrao,
    bool Ativo
);

public record ProdutoDetalheDto(
    Guid Id,
    string Codigo,
    string Descricao,
    string Ncm,
    string? Cest,
    string CfopPadrao,
    string UnidadeComercial,
    int OrigemMercadoria,
    decimal ValorUnitarioPadrao,
    string? CodigoEan,
    string? CodigoAnp,
    bool Ativo
);

// === CLIENTE ===
public record CreateClienteDto(
    int TipoPessoa,
    string? CpfCnpj,
    string RazaoSocial,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    string? InscricaoEstadual,
    int IndicadorIe
);

public record UpdateClienteDto(
    int TipoPessoa,
    string? CpfCnpj,
    string RazaoSocial,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    string? InscricaoEstadual,
    int IndicadorIe
);

public record ClienteResumoDto(
    Guid Id,
    int TipoPessoa,
    string? CpfCnpj,
    string RazaoSocial,
    string? NomeFantasia,
    string Uf,
    bool Ativo
);

public record ClienteDetalheDto(
    Guid Id,
    int TipoPessoa,
    string? CpfCnpj,
    string RazaoSocial,
    string? NomeFantasia,
    string? Email,
    string? Telefone,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    string? InscricaoEstadual,
    int IndicadorIe,
    bool Ativo
);

public record UpdateEmpresaDto(
    string RazaoSocial,
    string NomeFantasia,
    string InscricaoEstadual,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    string CodigoMunicipio,
    string Telefone,
    string Email,
    int RegimeTributario,
    int AmbienteSefaz,
    string? Cnae = null,
    string? CscId = null,
    string? CscToken = null
);

public record UploadCertificadoDto(string Senha);
public record CertificadoStatusDto(bool Valido, string? NomeTitular, string? Cnpj, DateTime? Validade, string? Mensagem);

public record ConfiguracaoEmpresaDto(
    int PerfilCliente,
    int TipoProduto,
    int VolumeNotas,
    int NivelAutomacao,
    bool EmiteParaConsumidorFinal,
    bool OperaIcmsSt,
    int NivelRelatorio,
    DateTime? ConcluidoEm
);

public record EmpresaDetalheDto(
    Guid Id,
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string InscricaoEstadual,
    string Email,
    string? Telefone,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Uf,
    string Cep,
    int RegimeTributario,
    int AmbienteSefaz,
    int UltimoNumeronFe,
    int UltimoNumeronFCe,
    DateTime? CertificadoValidade,
    string? CertificadoCnpj,
    bool CertificadoValido,
    string? Cnae = null,
    string? CodigoMunicipio = null,
    string? CscId = null,
    bool TemCscToken = false  // não expomos o token; apenas se está configurado
);
