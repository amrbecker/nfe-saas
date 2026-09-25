namespace NfeSaas.Domain.Enums;

// Enums do assistente Ori (docs/assistente/). Valores explícitos: persistidos como int.

/// <summary>Nível da fonte de uma regra (BASE_CONHECIMENTO.md §1.1). Define o selo e a linguagem da resposta.</summary>
public enum NivelFonte
{
    N1NormaOficial = 1,
    N2OrientacaoOficial = 2,
    N3ReferenciaTecnica = 3,
    N4SemFonte = 4,
    /// <summary>Guia de uso do próprio NFeFlow (artigos kb/sistema/*) — selo "Guia do NFeFlow", nunca "Norma oficial".</summary>
    GuiaDoSistema = 5
}

public enum TipoInteracaoAssistente
{
    ExplicarRejeicao = 1,
    PerguntaLivre = 2,
    MensagemConversa = 3
}

public enum PapelMensagem
{
    Usuario = 1,
    Assistente = 2,
    Ferramenta = 3
}

public enum SeveridadeChamado
{
    Baixa = 1,
    Media = 2,
    Alta = 3,
    Critica = 4
}

public enum StatusChamado
{
    Aberto = 1,
    EmTriagem = 2,
    IssueCriada = 3,
    Resolvido = 4,
    Descartado = 5
}

public enum TipoSinalProduto
{
    Dor = 1,
    PedidoFuncionalidade = 2,
    FriccaoUX = 3,
    LacunaConhecimento = 4,
    Elogio = 5
}

public enum TipoAlertaCs
{
    CertificadoVencendo = 1,
    RejeicaoRepetida = 2,
    OnboardingParado = 3,
    TrialAcabando = 4,
    SoHomologacao = 5,
    NotaRecorrente = 6,
    UsoCaindo = 7,          // interno: não é exibido ao usuário
    LembreteEmissao = 8
}

public enum PeriodicidadeLembrete
{
    Semanal = 1,
    Quinzenal = 2,
    Mensal = 3
}

public enum MecanismoMonitoramento
{
    HashPagina = 1,
    Inlabs = 2,
    Rss = 3
}

public enum StatusPublicacao
{
    Nova = 1,
    Descartada = 2,
    EmCuradoria = 3,
    Incorporada = 4
}
