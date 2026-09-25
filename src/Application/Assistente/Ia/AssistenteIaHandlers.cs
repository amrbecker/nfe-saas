using MediatR;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Ia;

/// <summary>Resultado padrão: null = recurso não encontrado (ou de outro tenant) / Ori desabilitada.</summary>
public record ExplicarRejeicaoCommand(EscopoOri Escopo, Guid NotaId, ContextoTelaDto? Contexto) : IRequest<RespostaOriDto?>;

public record PerguntarOriCommand(EscopoOri Escopo, string Pergunta, ContextoTelaDto? Contexto) : IRequest<RespostaOriDto?>;

public record ListarConversasQuery(EscopoOri Escopo) : IRequest<List<ConversaResumoDto>>;

public record CriarConversaCommand(EscopoOri Escopo, string? Titulo) : IRequest<ConversaResumoDto?>;

public record ListarMensagensConversaQuery(EscopoOri Escopo, Guid ConversaId) : IRequest<List<MensagemConversaDto>?>;

public record EnviarMensagemConversaCommand(EscopoOri Escopo, Guid ConversaId, NovaMensagemDto Mensagem,
    IProgress<EventoConversaDto>? Progresso) : IRequest<RespostaOriDto?>;

public record AvaliarInteracaoCommand(EscopoOri Escopo, Guid InteracaoId, int Valor) : IRequest<bool>;

public class ExplicarRejeicaoHandler : IRequestHandler<ExplicarRejeicaoCommand, RespostaOriDto?>
{
    private readonly ServicoRespostaOri _servico;
    private readonly MontadorContexto _contexto;
    public ExplicarRejeicaoHandler(ServicoRespostaOri servico, MontadorContexto contexto) { _servico = servico; _contexto = contexto; }

    public async Task<RespostaOriDto?> Handle(ExplicarRejeicaoCommand r, CancellationToken ct)
    {
        if (!_servico.Habilitado(r.Escopo.EscritorioId)) return null;
        var nota = await _contexto.ObterNotaAsync(r.Escopo, r.NotaId, ct);
        if (nota == null) return null;
        var pergunta = nota.Situacao == SituacaoNota.Rejeitada
            ? "Por que esta nota foi rejeitada e o que devo corrigir no NFeFlow?"
            : "Explique a situação desta nota e o que fazer a seguir.";
        return await _servico.ResponderAsync(new PedidoRespostaOri(r.Escopo, TipoInteracaoAssistente.ExplicarRejeicao,
            pergunta, r.Contexto, nota.Id), null, ct);
    }
}

public class PerguntarOriHandler : IRequestHandler<PerguntarOriCommand, RespostaOriDto?>
{
    private readonly ServicoRespostaOri _servico;
    public PerguntarOriHandler(ServicoRespostaOri servico) => _servico = servico;

    public async Task<RespostaOriDto?> Handle(PerguntarOriCommand r, CancellationToken ct)
    {
        if (!_servico.Habilitado(r.Escopo.EscritorioId) || string.IsNullOrWhiteSpace(r.Pergunta)) return null;
        var pergunta = r.Pergunta.Length > 2000 ? r.Pergunta[..2000] : r.Pergunta;
        return await _servico.ResponderAsync(new PedidoRespostaOri(r.Escopo, TipoInteracaoAssistente.PerguntaLivre,
            pergunta, r.Contexto), null, ct);
    }
}

public class ConversasHandlers :
    IRequestHandler<ListarConversasQuery, List<ConversaResumoDto>>,
    IRequestHandler<CriarConversaCommand, ConversaResumoDto?>,
    IRequestHandler<ListarMensagensConversaQuery, List<MensagemConversaDto>?>,
    IRequestHandler<EnviarMensagemConversaCommand, RespostaOriDto?>
{
    public const int LimiteTurnos = 20;

    private readonly IConversaRepository _conversas;
    private readonly IUnitOfWork _uow;
    private readonly ServicoRespostaOri _servico;
    private readonly ISanitizadorIA _sanitizador;

    public ConversasHandlers(IConversaRepository conversas, IUnitOfWork uow, ServicoRespostaOri servico, ISanitizadorIA sanitizador)
    {
        _conversas = conversas;
        _uow = uow;
        _servico = servico;
        _sanitizador = sanitizador;
    }

    public async Task<List<ConversaResumoDto>> Handle(ListarConversasQuery r, CancellationToken ct) =>
        (await _conversas.ListarPorUsuarioAsync(r.Escopo.UsuarioId, r.Escopo.EmpresaId, 20, ct))
            .Select(c => new ConversaResumoDto(c.Id, c.Titulo, c.UltimaMensagemEm)).ToList();

    public async Task<ConversaResumoDto?> Handle(CriarConversaCommand r, CancellationToken ct)
    {
        if (!_servico.Habilitado(r.Escopo.EscritorioId)) return null;
        var titulo = _sanitizador.Sanitizar(r.Titulo ?? "Conversa com a Ori").Texto;
        var conversa = Conversa.Criar(r.Escopo.EscritorioId, r.Escopo.EmpresaId, r.Escopo.UsuarioId, titulo);
        await _conversas.AddAsync(conversa, ct);
        await _uow.SaveChangesAsync(ct);
        return new ConversaResumoDto(conversa.Id, conversa.Titulo, conversa.UltimaMensagemEm);
    }

    public async Task<List<MensagemConversaDto>?> Handle(ListarMensagensConversaQuery r, CancellationToken ct)
    {
        var conversa = await Obter(r.Escopo, r.ConversaId, ct);
        // Histórico gravado é o sanitizado (marcadores): os valores reais só existiram na tela durante a requisição.
        return conversa?.Mensagens.Where(m => m.Papel != PapelMensagem.Ferramenta).OrderBy(m => m.CriadaEm)
            .Select(m => new MensagemConversaDto(m.Papel == PapelMensagem.Usuario ? "usuario" : "ori", m.ConteudoSanitizado, m.CriadaEm))
            .ToList();
    }

    public async Task<RespostaOriDto?> Handle(EnviarMensagemConversaCommand r, CancellationToken ct)
    {
        if (!_servico.Habilitado(r.Escopo.EscritorioId) || string.IsNullOrWhiteSpace(r.Mensagem.Texto)) return null;
        var conversa = await Obter(r.Escopo, r.ConversaId, ct);
        if (conversa == null) return null;
        if (conversa.TotalTurnos >= LimiteTurnos)
            return new RespostaOriDto(null,
                "Esta conversa ficou longa. Para eu continuar com precisão, comece uma nova conversa — o contexto da tela vai junto.",
                NivelFonte.N4SemFonte, new(), false, true, 0, new(), null);

        var texto = r.Mensagem.Texto.Length > 2000 ? r.Mensagem.Texto[..2000] : r.Mensagem.Texto;
        return await _servico.ResponderAsync(new PedidoRespostaOri(r.Escopo, TipoInteracaoAssistente.MensagemConversa,
            texto, r.Mensagem.Contexto, Conversa: conversa), r.Progresso, ct);
    }

    private async Task<Conversa?> Obter(EscopoOri e, Guid id, CancellationToken ct)
    {
        var c = await _conversas.GetByIdComMensagensAsync(id, ct);
        return c != null && c.UsuarioId == e.UsuarioId && c.EmpresaId == e.EmpresaId ? c : null;
    }
}

public class AvaliarInteracaoHandler : IRequestHandler<AvaliarInteracaoCommand, bool>
{
    private readonly IInteracaoAssistenteRepository _interacoes;
    private readonly IUnitOfWork _uow;
    public AvaliarInteracaoHandler(IInteracaoAssistenteRepository interacoes, IUnitOfWork uow) { _interacoes = interacoes; _uow = uow; }

    public async Task<bool> Handle(AvaliarInteracaoCommand r, CancellationToken ct)
    {
        if (r.Valor is not (1 or -1)) return false;
        var i = await _interacoes.GetByIdAsync(r.InteracaoId, ct);
        if (i == null || i.UsuarioId != r.Escopo.UsuarioId) return false;
        i.Avaliar(r.Valor);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
