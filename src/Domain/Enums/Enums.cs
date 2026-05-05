namespace NfeSaas.Domain.Enums;

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
