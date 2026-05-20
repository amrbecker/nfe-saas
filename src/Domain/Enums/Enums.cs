namespace NfeSaas.Domain.Enums;

// Não existe plano gratuito. Todo novo escritório recebe trial de 30 dias do plano escolhido;
// após o trial, o login é bloqueado até pagamento e ativação (ver Escritorio.AtivarPlanoPago).
public enum PlanoSaas
{
    Basico = 1,
    Profissional = 2,
    Enterprise = 3
}

// Estado de cobrança do escritório, derivado de TrialFimEm + PlanoAtivoAteEm.
public enum StatusAssinaturaEscritorio
{
    TrialAtivo = 1,
    Pago = 2,
    TrialExpirado = 3,
    Suspenso = 4
}

public enum TipoNota
{
    NFe = 55,
    NFCe = 65
}

public enum SituacaoNota
{
    Rascunho = 0,
    Enviada = 1,
    Autorizada = 2,
    Cancelada = 3,
    Denegada = 4,
    Rejeitada = 5
}

public enum AmbienteSefaz
{
    Producao = 1,
    Homologacao = 2
}

public enum TipoOperacao
{
    Entrada = 0,
    Saida = 1
}

public enum ModalidadeFrete
{
    ContratacaoRemetente = 0,
    ContratacaoDestinatario = 1,
    ContratacaoTerceiros = 2,
    ProprioRemetente = 3,
    ProprioDestinatario = 4,
    SemFrete = 9
}

public enum FinalidadeNota
{
    Normal = 1,
    Complementar = 2,
    Ajuste = 3,
    Devolucao = 4
}

public enum TipoPessoa
{
    PessoaFisica = 1,
    PessoaJuridica = 2,
    Estrangeiro = 3
}

public enum RegimeTributario
{
    SimplesNacional = 1,
    SimplesNacionalExcessoSublimite = 2,
    RegimeNormal = 3
}

public enum OrigemMercadoria
{
    Nacional = 0,
    EstrangeiraImportacaoDireta = 1,
    EstrangeiraAdquiridaMercadoInterno = 2,
    NacionalConteudoImportacaoSuperior40 = 3,
    NacionalProcessosBasicos = 4,
    NacionalConteudoImportacaoInferior40 = 5,
    EstrangeiraImportacaoDiretaSemSimilar = 6,
    EstrangeiraAdquiridaMercadoInternoSemSimilar = 7,
    NacionalConteudoImportacaoSuperior70 = 8
}

public enum CstIcms
{
    Tributada = 00,
    TributadaComCobrancaDifal = 10,
    ComReducaoBaseCalculo = 20,
    NaoTributada = 30,
    Isenta = 40,
    Suspensao = 50,
    Diferimento = 51,
    CobrancaIcmsSt = 60,
    ComReducaoBaseCalculoSt = 70,
    Outras = 90
}

// CSOSN — Código de Situação da Operação no Simples Nacional (Anexo III do RICMS)
public enum CsosnIcms
{
    TributadaComPermissaoCredito = 101,
    TributadaSemPermissaoCredito = 102,
    IsencaoIcmsFaixaReceitaBruta = 103,
    TributadaComPermissaoCreditoSt = 201,
    TributadaSemPermissaoCreditoSt = 202,
    IsencaoIcmsFaixaReceitaBrutaSt = 203,
    Imune = 300,
    NaoTributada = 400,
    IcmsCobradoAnteriormentePorSt = 500,
    Outros = 900
}

public enum TipoEmissao
{
    Normal = 1,
    ContingenciaSvcAn = 9,   // SVC-AN (SEFAZ Virtual Contingência - AN)
    ContingenciaSvcRs = 6,   // SVC-RS (SEFAZ Virtual Contingência - RS)
    ContingenciaFsda = 5,    // FS-DA (Formulário de Segurança - Documento Auxiliar)
}

public enum CstPisCofins
{
    TributadaAliquotaBasica = 01,
    TributadaAliquotaDiferenciada = 02,
    TributadaAliquotaUnidade = 03,
    MonofasicaAliquotaZero = 04,
    SubstituicaoTributaria = 05,
    AliquotaZero = 06,
    IsentaContribuicao = 07,
    SemIncidenciaContribuicao = 08,
    SuspensaoContribuicao = 09,
    Outras = 49,
    NaoTributada = 99
}

public enum PerfilCliente
{
    PequenasEmpresasSimples = 1,
    EmpresasMistasComSt = 2,
    ClientesExigentesComplexos = 3
}

public enum TipoProduto
{
    ServicosBasicos = 1,
    ProdutosSimples = 2,
    ProdutosComplexos = 3
}

public enum VolumeNotas
{
    Ate100 = 1,
    De101A1000 = 2,
    MaisDe1000 = 3
}

public enum NivelAutomacao
{
    Manual = 1,
    SemiAutomatico = 2,
    Automatico = 3
}

public enum NivelRelatorio
{
    Basico = 1,
    Intermediario = 2,
    Avancado = 3
}

// Códigos tpEvento SEFAZ (Manual de Eventos NFe).
// Inutilização não é "evento" técnico (serviço próprio NfeInutilizacao), mas modelamos junto por simplicidade.
public enum TipoEventoFiscal
{
    CartaCorrecao = 110110,
    Cancelamento = 110111,
    Inutilizacao = 999000,                   // sintético — Inutilização tem serviço SEFAZ separado
    ManifestacaoConfirmacao = 210200,
    ManifestacaoCiencia = 210210,
    ManifestacaoDesconhecimento = 210220,
    ManifestacaoOperacaoNaoRealizada = 210240
}

public enum SituacaoEventoFiscal
{
    Registrado = 0,
    Aceito = 1,
    Rejeitado = 2
}
