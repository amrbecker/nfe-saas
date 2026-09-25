using NfeSaas.Domain.Common;
using NfeSaas.Domain.Enums;

namespace NfeSaas.Domain.Entities;

// Entidades do assistente Ori (docs/assistente/). Regra transversal: nenhum texto aqui guarda dado
// pessoal cru — perguntas, respostas, resumos e contextos são gravados JÁ sanitizados (SanitizadorIA,
// PESQUISA_REFINAMENTO.md §2.6).

/// <summary>Telemetria de produto (Fase 0.1). Nunca guarda valores de campos — só evento e tela.</summary>
public class EventoProduto
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EscritorioId { get; private set; }
    public Guid? EmpresaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Tipo { get; private set; } = null!;      // ex.: "tela_aberta", "emissao_rejeitada"
    public string? Tela { get; private set; }               // rota semântica, ex.: "emitir-nfe"
    public string? DadosJson { get; private set; }          // metadados sem PII (ex.: {"cStat":"778"})
    public DateTime OcorridoEm { get; private set; }

    protected EventoProduto() { }

    public static EventoProduto Criar(Guid escritorioId, Guid? empresaId, Guid usuarioId, string tipo,
        string? tela, string? dadosJson, DateTime ocorridoEm)
    {
        if (string.IsNullOrWhiteSpace(tipo)) throw new ArgumentException("Tipo é obrigatório.", nameof(tipo));
        return new EventoProduto
        {
            EscritorioId = escritorioId,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Tipo = tipo.Trim(),
            Tela = tela?.Trim(),
            DadosJson = dadosJson,
            OcorridoEm = ocorridoEm
        };
    }
}

/// <summary>Preferências do usuário em relação à Ori (ex.: silenciar dicas).</summary>
public class PreferenciaAssistente : BaseEntity
{
    public Guid UsuarioId { get; private set; }
    public bool DicasSilenciadas { get; private set; }

    protected PreferenciaAssistente() { }

    public static PreferenciaAssistente Criar(Guid usuarioId) => new() { UsuarioId = usuarioId };

    public void DefinirDicasSilenciadas(bool silenciadas) { DicasSilenciadas = silenciadas; SetUpdated(); }
}

/// <summary>
/// Controle de repetição de dicas e sugestões (MASCOTE_UX.md §5, AUTOMACOES.md §2):
/// dispensada ("✕") não volta; ignorada 3 vezes fica suspensa por 30 dias.
/// </summary>
public class SugestaoDispensada : BaseEntity
{
    public const int LimiteIgnoradas = 3;
    public const int DiasSuspensao = 30;

    public Guid UsuarioId { get; private set; }
    public Guid? EmpresaId { get; private set; }
    public string Chave { get; private set; } = null!;     // ex.: "A6:cliente:{id}:produto:{cod}:cfop:5102"
    public int VezesIgnorada { get; private set; }
    public bool DispensadaDefinitivamente { get; private set; }
    public DateTime? SuspensaAte { get; private set; }

    protected SugestaoDispensada() { }

    public static SugestaoDispensada Criar(Guid usuarioId, Guid? empresaId, string chave)
    {
        if (string.IsNullOrWhiteSpace(chave)) throw new ArgumentException("Chave é obrigatória.", nameof(chave));
        return new SugestaoDispensada { UsuarioId = usuarioId, EmpresaId = empresaId, Chave = chave.Trim() };
    }

    public void Dispensar() { DispensadaDefinitivamente = true; SetUpdated(); }

    public void RegistrarIgnorada(DateTime agoraUtc)
    {
        VezesIgnorada++;
        if (VezesIgnorada >= LimiteIgnoradas)
        {
            SuspensaAte = agoraUtc.AddDays(DiasSuspensao);
            VezesIgnorada = 0;
        }
        SetUpdated();
    }

    public bool EstaBloqueada(DateTime agoraUtc) =>
        DispensadaDefinitivamente || (SuspensaAte.HasValue && SuspensaAte.Value > agoraUtc);
}

/// <summary>Consumo diário do modelo por usuário — base das cotas (PESQUISA_REFINAMENTO.md §2.5).</summary>
public class UsoAssistente
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UsuarioId { get; private set; }
    public Guid EscritorioId { get; private set; }
    public DateOnly Dia { get; private set; }
    public int Mensagens { get; private set; }
    public long TokensEntrada { get; private set; }
    public long TokensCache { get; private set; }
    public long TokensSaida { get; private set; }
    public decimal CustoEstimadoUsd { get; private set; }

    protected UsoAssistente() { }

    public static UsoAssistente Criar(Guid usuarioId, Guid escritorioId, DateOnly dia) =>
        new() { UsuarioId = usuarioId, EscritorioId = escritorioId, Dia = dia };

    public void Registrar(long tokensEntrada, long tokensCache, long tokensSaida, decimal custoUsd)
    {
        Mensagens++;
        TokensEntrada += tokensEntrada;
        TokensCache += tokensCache;
        TokensSaida += tokensSaida;
        CustoEstimadoUsd += custoUsd;
    }
}

/// <summary>Registro de cada resposta do modelo (pergunta/resposta sanitizadas), com custo e avaliação 👍/👎.</summary>
public class InteracaoAssistente : BaseEntity
{
    public Guid EscritorioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid? ConversaId { get; private set; }
    public Guid? NotaFiscalId { get; private set; }
    public TipoInteracaoAssistente Tipo { get; private set; }
    public string PerguntaSanitizada { get; private set; } = null!;
    public string RespostaSanitizada { get; private set; } = null!;
    public string? ArtigosCitados { get; private set; }     // ids separados por vírgula
    public NivelFonte Selo { get; private set; }
    public string Modelo { get; private set; } = null!;
    public long TokensEntrada { get; private set; }
    public long TokensCache { get; private set; }
    public long TokensSaida { get; private set; }
    public decimal CustoEstimadoUsd { get; private set; }
    public bool VerificacaoFalhou { get; private set; }     // verificador substituiu por resposta N4
    public int? Avaliacao { get; private set; }             // +1 👍 / -1 👎

    protected InteracaoAssistente() { }

    public static InteracaoAssistente Criar(Guid escritorioId, Guid empresaId, Guid usuarioId,
        TipoInteracaoAssistente tipo, string perguntaSanitizada, string respostaSanitizada,
        IEnumerable<string> artigosCitados, NivelFonte selo, string modelo,
        long tokensEntrada, long tokensCache, long tokensSaida, decimal custoUsd,
        bool verificacaoFalhou, Guid? conversaId = null, Guid? notaFiscalId = null) => new()
    {
        EscritorioId = escritorioId,
        EmpresaId = empresaId,
        UsuarioId = usuarioId,
        Tipo = tipo,
        PerguntaSanitizada = perguntaSanitizada,
        RespostaSanitizada = respostaSanitizada,
        ArtigosCitados = string.Join(',', artigosCitados.Distinct()),
        Selo = selo,
        Modelo = modelo,
        TokensEntrada = tokensEntrada,
        TokensCache = tokensCache,
        TokensSaida = tokensSaida,
        CustoEstimadoUsd = custoUsd,
        VerificacaoFalhou = verificacaoFalhou,
        ConversaId = conversaId,
        NotaFiscalId = notaFiscalId
    };

    public void Avaliar(int valor)
    {
        if (valor is not (1 or -1)) throw new ArgumentOutOfRangeException(nameof(valor), "Use +1 ou -1.");
        Avaliacao = valor;
        SetUpdated();
    }
}

/// <summary>Conversa com a Ori. Retenção de 180 dias (expurgo por worker).</summary>
public class Conversa : BaseEntity
{
    public const int DiasRetencao = 180;

    public Guid EscritorioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string? ResumoAnterior { get; private set; }     // resumo dos turnos fora da janela (E6)
    public DateTime UltimaMensagemEm { get; private set; }
    public int TotalTurnos { get; private set; }

    private readonly List<MensagemConversa> _mensagens = new();
    public IReadOnlyCollection<MensagemConversa> Mensagens => _mensagens.AsReadOnly();

    protected Conversa() { }

    public static Conversa Criar(Guid escritorioId, Guid empresaId, Guid usuarioId, string titulo) => new()
    {
        EscritorioId = escritorioId,
        EmpresaId = empresaId,
        UsuarioId = usuarioId,
        Titulo = string.IsNullOrWhiteSpace(titulo) ? "Conversa com a Ori" : titulo.Trim()[..Math.Min(titulo.Trim().Length, 120)],
        UltimaMensagemEm = DateTime.UtcNow
    };

    public MensagemConversa AdicionarMensagem(PapelMensagem papel, string conteudoSanitizado, string? ferramentasJson = null)
    {
        var msg = MensagemConversa.Criar(Id, papel, conteudoSanitizado, ferramentasJson);
        _mensagens.Add(msg);
        if (papel == PapelMensagem.Usuario) TotalTurnos++;
        UltimaMensagemEm = msg.CriadaEm;
        SetUpdated();
        return msg;
    }

    public void AtualizarResumo(string resumoSanitizado) { ResumoAnterior = resumoSanitizado; SetUpdated(); }
}

public class MensagemConversa
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ConversaId { get; private set; }
    public PapelMensagem Papel { get; private set; }
    public string ConteudoSanitizado { get; private set; } = null!;
    public string? FerramentasJson { get; private set; }    // chamadas/resultados de ferramentas (sanitizados)
    public DateTime CriadaEm { get; private set; } = DateTime.UtcNow;

    protected MensagemConversa() { }

    internal static MensagemConversa Criar(Guid conversaId, PapelMensagem papel, string conteudo, string? ferramentasJson) => new()
    {
        ConversaId = conversaId,
        Papel = papel,
        ConteudoSanitizado = conteudo,
        FerramentasJson = ferramentasJson
    };
}

/// <summary>Chamado de bug aberto pela Ori com confirmação do usuário; triado por humano (role Plataforma).</summary>
public class Chamado : BaseEntity
{
    public Guid EscritorioId { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string? Passos { get; private set; }
    public SeveridadeChamado Severidade { get; private set; }
    public Guid? NotaFiscalId { get; private set; }
    public string? ContextoTecnicoJson { get; private set; }  // rota, versão, Sentry EventId, erros, rastro — sanitizado
    public StatusChamado Status { get; private set; } = StatusChamado.Aberto;
    public string? IssueUrl { get; private set; }
    public string? NotaTriagem { get; private set; }

    protected Chamado() { }

    public static Chamado Criar(Guid escritorioId, Guid empresaId, Guid usuarioId, string titulo, string descricao,
        string? passos, SeveridadeChamado severidade, Guid? notaFiscalId, string? contextoTecnicoJson)
    {
        if (string.IsNullOrWhiteSpace(titulo)) throw new ArgumentException("Título é obrigatório.", nameof(titulo));
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));
        return new Chamado
        {
            EscritorioId = escritorioId,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Titulo = titulo.Trim(),
            Descricao = descricao.Trim(),
            Passos = passos?.Trim(),
            Severidade = severidade,
            NotaFiscalId = notaFiscalId,
            ContextoTecnicoJson = contextoTecnicoJson
        };
    }

    public void AtualizarTriagem(StatusChamado status, string? issueUrl, string? nota)
    {
        Status = status;
        IssueUrl = string.IsNullOrWhiteSpace(issueUrl) ? IssueUrl : issueUrl.Trim();
        NotaTriagem = nota?.Trim();
        SetUpdated();
    }
}

/// <summary>Sinal de produto (dor, pedido, fricção, lacuna de conhecimento, elogio) — resumo já sanitizado.</summary>
public class SinalProduto : BaseEntity
{
    public Guid? EscritorioId { get; private set; }
    public Guid? EmpresaId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public TipoSinalProduto Tipo { get; private set; }
    public string ResumoSanitizado { get; private set; } = null!;
    public string? Tela { get; private set; }
    public string? Tema { get; private set; }               // agrupador (ex.: "cClassTrib", "cfop") para agregação

    protected SinalProduto() { }

    public static SinalProduto Criar(Guid? escritorioId, Guid? empresaId, Guid? usuarioId, TipoSinalProduto tipo,
        string resumoSanitizado, string? tela, string? tema)
    {
        if (string.IsNullOrWhiteSpace(resumoSanitizado)) throw new ArgumentException("Resumo é obrigatório.", nameof(resumoSanitizado));
        return new SinalProduto
        {
            EscritorioId = escritorioId,
            EmpresaId = empresaId,
            UsuarioId = usuarioId,
            Tipo = tipo,
            ResumoSanitizado = resumoSanitizado.Trim(),
            Tela = tela?.Trim(),
            Tema = tema?.Trim().ToLowerInvariant()
        };
    }
}

/// <summary>
/// Alerta de Customer Success gerado por regras determinísticas (SaudeContaWorker). <see cref="Chave"/> é
/// única e deduplica (ex.: "cert:{empresaId}:7"). Alertas <see cref="Interno"/> não aparecem ao usuário.
/// </summary>
public class AlertaCs : BaseEntity
{
    public Guid EscritorioId { get; private set; }
    public Guid? EmpresaId { get; private set; }
    public TipoAlertaCs Tipo { get; private set; }
    public string Chave { get; private set; } = null!;
    public string Mensagem { get; private set; } = null!;
    public string? LinkAcao { get; private set; }           // rota da WebUI, ex.: "/certificado", "/emitir?origem={id}"
    public bool Interno { get; private set; }
    public DateTime? VistoEm { get; private set; }
    public DateTime? DispensadoEm { get; private set; }
    public DateTime? EmailEnviadoEm { get; private set; }

    protected AlertaCs() { }

    public static AlertaCs Criar(Guid escritorioId, Guid? empresaId, TipoAlertaCs tipo, string chave, string mensagem,
        string? linkAcao, bool interno = false)
    {
        if (string.IsNullOrWhiteSpace(chave)) throw new ArgumentException("Chave é obrigatória.", nameof(chave));
        if (string.IsNullOrWhiteSpace(mensagem)) throw new ArgumentException("Mensagem é obrigatória.", nameof(mensagem));
        return new AlertaCs
        {
            EscritorioId = escritorioId,
            EmpresaId = empresaId,
            Tipo = tipo,
            Chave = chave.Trim(),
            Mensagem = mensagem.Trim(),
            LinkAcao = linkAcao,
            Interno = interno
        };
    }

    public void MarcarVisto() { VistoEm ??= DateTime.UtcNow; SetUpdated(); }
    public void Dispensar() { DispensadoEm = DateTime.UtcNow; SetUpdated(); }
    public void MarcarEmailEnviado() { EmailEnviadoEm = DateTime.UtcNow; SetUpdated(); }
    public bool Ativo => DispensadoEm == null;
}

/// <summary>
/// Lembrete de nota recorrente (AUTOMACOES.md A7). No dia, o worker gera um AlertaCs com link para
/// "/emitir?origem={NotaModeloId}" — o formulário abre preparado e o usuário revisa e emite. Nunca transmite.
/// </summary>
public class LembreteEmissao : BaseEntity
{
    public Guid EmpresaId { get; private set; }
    public Guid CriadoPorUsuarioId { get; private set; }
    public Guid NotaModeloId { get; private set; }
    public string Descricao { get; private set; } = null!;
    public PeriodicidadeLembrete Periodicidade { get; private set; }
    public int Dia { get; private set; }                    // Mensal: 1–28; Semanal/Quinzenal: 0 (dom) – 6 (sáb)
    public DateTime ProximaEm { get; private set; }
    public DateTime? UltimoAvisoEm { get; private set; }
    public bool Ativo { get; private set; } = true;

    protected LembreteEmissao() { }

    public static LembreteEmissao Criar(Guid empresaId, Guid usuarioId, Guid notaModeloId, string descricao,
        PeriodicidadeLembrete periodicidade, int dia, DateTime agoraUtc)
    {
        ValidarDia(periodicidade, dia);
        var l = new LembreteEmissao
        {
            EmpresaId = empresaId,
            CriadoPorUsuarioId = usuarioId,
            NotaModeloId = notaModeloId,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? "Nota recorrente" : descricao.Trim(),
            Periodicidade = periodicidade,
            Dia = dia
        };
        l.ProximaEm = CalcularProxima(periodicidade, dia, agoraUtc);
        return l;
    }

    public void Atualizar(string descricao, PeriodicidadeLembrete periodicidade, int dia, DateTime agoraUtc)
    {
        ValidarDia(periodicidade, dia);
        Descricao = string.IsNullOrWhiteSpace(descricao) ? Descricao : descricao.Trim();
        Periodicidade = periodicidade;
        Dia = dia;
        ProximaEm = CalcularProxima(periodicidade, dia, agoraUtc);
        SetUpdated();
    }

    public void RegistrarAviso(DateTime agoraUtc)
    {
        UltimoAvisoEm = agoraUtc;
        var proxima = Periodicidade switch
        {
            PeriodicidadeLembrete.Semanal => ProximaEm.AddDays(7),
            PeriodicidadeLembrete.Quinzenal => ProximaEm.AddDays(14),
            _ => ProximaEm.AddMonths(1)
        };
        // Se o worker ficou parado por muito tempo, avança até uma data futura.
        ProximaEm = proxima > agoraUtc ? proxima : CalcularProxima(Periodicidade, Dia, agoraUtc.Date.AddDays(1));
        SetUpdated();
    }

    public void Desativar() { Ativo = false; SetUpdated(); }
    public void Ativar(DateTime agoraUtc) { Ativo = true; ProximaEm = CalcularProxima(Periodicidade, Dia, agoraUtc); SetUpdated(); }

    private static void ValidarDia(PeriodicidadeLembrete p, int dia)
    {
        if (p == PeriodicidadeLembrete.Mensal && dia is < 1 or > 28)
            throw new ArgumentOutOfRangeException(nameof(dia), "Para lembrete mensal, use dia entre 1 e 28.");
        if (p != PeriodicidadeLembrete.Mensal && dia is < 0 or > 6)
            throw new ArgumentOutOfRangeException(nameof(dia), "Para lembrete semanal/quinzenal, use dia da semana entre 0 e 6.");
    }

    /// <summary>Próxima data (00:00 UTC) a partir de <paramref name="aPartirDe"/>, inclusive o próprio dia.</summary>
    public static DateTime CalcularProxima(PeriodicidadeLembrete p, int dia, DateTime aPartirDe)
    {
        var d = aPartirDe.Date;
        if (p == PeriodicidadeLembrete.Mensal)
        {
            var candidato = new DateTime(d.Year, d.Month, dia, 0, 0, 0, DateTimeKind.Utc);
            return candidato >= DateTime.SpecifyKind(d, DateTimeKind.Utc) ? candidato : candidato.AddMonths(1);
        }
        var delta = ((dia - (int)d.DayOfWeek) + 7) % 7;
        return DateTime.SpecifyKind(d.AddDays(delta), DateTimeKind.Utc);
    }
}

/// <summary>Fonte oficial ou de referência vigiada pelo MonitorFontesWorker (MONITORAMENTO_FONTES.md §2).</summary>
public class FonteMonitorada : BaseEntity
{
    public string Nome { get; private set; } = null!;
    public NivelFonte Nivel { get; private set; }
    public string Url { get; private set; } = null!;
    public MecanismoMonitoramento Mecanismo { get; private set; }
    public int FrequenciaHoras { get; private set; }
    public string? TermosFiltro { get; private set; }       // separados por ";" (usado no INLABS/DOU)
    public string? UltimoHash { get; private set; }
    public DateTime? UltimaLeituraEm { get; private set; }
    public DateTime? UltimaMudancaEm { get; private set; }
    public int FalhasSeguidas { get; private set; }
    public bool Ativa { get; private set; } = true;

    protected FonteMonitorada() { }

    public static FonteMonitorada Criar(string nome, NivelFonte nivel, string url, MecanismoMonitoramento mecanismo,
        int frequenciaHoras, string? termosFiltro = null) => new()
    {
        Nome = nome.Trim(),
        Nivel = nivel,
        Url = url.Trim(),
        Mecanismo = mecanismo,
        FrequenciaHoras = Math.Max(1, frequenciaHoras),
        TermosFiltro = termosFiltro
    };

    public bool DevidaEm(DateTime agoraUtc) => Ativa && (UltimaLeituraEm == null || UltimaLeituraEm.Value.AddHours(FrequenciaHoras) <= agoraUtc);

    /// <summary>Registra leitura bem-sucedida. Retorna true se o conteúdo mudou desde a última leitura.</summary>
    public bool RegistrarLeitura(string hash, DateTime agoraUtc)
    {
        var mudou = UltimoHash != null && UltimoHash != hash;
        UltimoHash = hash;
        UltimaLeituraEm = agoraUtc;
        FalhasSeguidas = 0;
        if (mudou) UltimaMudancaEm = agoraUtc;
        SetUpdated();
        return mudou;
    }

    public void RegistrarFalha(DateTime agoraUtc) { FalhasSeguidas++; UltimaLeituraEm = agoraUtc; SetUpdated(); }

    /// <summary>"Fonte cega": 3 leituras seguidas falharam (HTML mudou ou site fora).</summary>
    public bool Cega => FalhasSeguidas >= 3;

    public void DefinirAtiva(bool ativa) { Ativa = ativa; SetUpdated(); }
}

/// <summary>Novidade detectada numa fonte monitorada — fila de curadoria do escritório parceiro.</summary>
public class PublicacaoDetectada : BaseEntity
{
    public Guid FonteId { get; private set; }
    public FonteMonitorada Fonte { get; private set; } = null!;
    public string Titulo { get; private set; } = null!;
    public string? Url { get; private set; }
    public string Hash { get; private set; } = null!;
    public DateTime DetectadaEm { get; private set; }
    public bool? Relevante { get; private set; }            // null = ainda não classificada
    public string? Resumo { get; private set; }
    public DateTime? VigenciaInicio { get; private set; }
    public string? ArtigosAfetados { get; private set; }    // ids da base separados por vírgula
    public bool ImpactoTecnico { get; private set; }        // mudança de leiaute/XSD/URL → issue para dev
    public StatusPublicacao Status { get; private set; } = StatusPublicacao.Nova;
    public string? NotaCurador { get; private set; }

    protected PublicacaoDetectada() { }

    public static PublicacaoDetectada Criar(Guid fonteId, string titulo, string? url, string hash, DateTime detectadaEm) => new()
    {
        FonteId = fonteId,
        Titulo = titulo.Trim()[..Math.Min(titulo.Trim().Length, 500)],
        Url = url,
        Hash = hash,
        DetectadaEm = detectadaEm
    };

    public void Classificar(bool relevante, string? resumo, DateTime? vigenciaInicio, IEnumerable<string> artigosAfetados, bool impactoTecnico)
    {
        Relevante = relevante;
        Resumo = resumo;
        VigenciaInicio = vigenciaInicio;
        ArtigosAfetados = string.Join(',', artigosAfetados.Distinct());
        ImpactoTecnico = impactoTecnico;
        if (!relevante && Status == StatusPublicacao.Nova) Status = StatusPublicacao.Descartada;
        SetUpdated();
    }

    public void AtualizarStatus(StatusPublicacao status, string? notaCurador)
    {
        Status = status;
        NotaCurador = notaCurador?.Trim();
        SetUpdated();
    }
}
