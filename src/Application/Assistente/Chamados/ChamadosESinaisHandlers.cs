using System.Reflection;
using System.Text.Json;
using MediatR;
using NfeSaas.Application.DTOs.Assistente;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Interfaces.Assistente;

namespace NfeSaas.Application.Assistente.Chamados;

public record ChamadoInternoDto(Guid Id, string Titulo, string Descricao, string? Passos, SeveridadeChamado Severidade,
    StatusChamado Status, Guid EscritorioId, Guid EmpresaId, Guid? NotaFiscalId, string? ContextoTecnicoJson,
    string? IssueUrl, string? NotaTriagem, DateTime CriadoEm);

public record AtualizarChamadoDto(StatusChamado Status, string? IssueUrl, string? Nota);

public record ListarChamadosQuery(StatusChamado? Status, int Pagina, int Tamanho) : IRequest<List<ChamadoInternoDto>>;

public record AtualizarChamadoCommand(Guid Id, AtualizarChamadoDto Dto) : IRequest<bool>;

/// <summary>Sinal de produto — o resumo é sempre sanitizado antes de gravar.</summary>
public class RegistrarSinalHandler : IRequestHandler<RegistrarSinalCommand, Guid>
{
    private readonly ISinalProdutoRepository _sinais;
    private readonly ISanitizadorIA _sanitizador;
    private readonly IUnitOfWork _uow;

    public RegistrarSinalHandler(ISinalProdutoRepository sinais, ISanitizadorIA sanitizador, IUnitOfWork uow)
    {
        _sinais = sinais;
        _sanitizador = sanitizador;
        _uow = uow;
    }

    public async Task<Guid> Handle(RegistrarSinalCommand r, CancellationToken ct)
    {
        var resumo = _sanitizador.Sanitizar(r.Resumo).Texto.Trim();
        if (resumo.Length > 1900) resumo = resumo[..1900];
        var tema = string.IsNullOrWhiteSpace(r.Tema) ? null : new string(r.Tema.Trim().ToLowerInvariant().Take(80).ToArray());
        var sinal = SinalProduto.Criar(r.EscritorioId, r.EmpresaId, r.UsuarioId, r.Tipo, resumo, r.Tela, tema);
        await _sinais.AddAsync(sinal, ct);
        await _uow.SaveChangesAsync(ct);
        return sinal.Id;
    }
}

/// <summary>
/// Chamado confirmado pelo usuário. Texto e contexto técnico sanitizados; a nota precisa ser da empresa do token.
/// </summary>
public class CriarChamadoHandler : IRequestHandler<CriarChamadoCommand, Guid>
{
    private static readonly string Versao =
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "desconhecida";

    private readonly IChamadoRepository _chamados;
    private readonly INotaFiscalRepository _notas;
    private readonly ISanitizadorIA _sanitizador;
    private readonly IUnitOfWork _uow;

    public CriarChamadoHandler(IChamadoRepository chamados, INotaFiscalRepository notas, ISanitizadorIA sanitizador, IUnitOfWork uow)
    {
        _chamados = chamados;
        _notas = notas;
        _sanitizador = sanitizador;
        _uow = uow;
    }

    public async Task<Guid> Handle(CriarChamadoCommand r, CancellationToken ct)
    {
        var d = r.Dto;
        Guid? notaId = null;
        if (d.NotaId is { } id && await _notas.GetByIdAsync(id, ct) is { } nota && nota.EmpresaId == r.EmpresaId)
            notaId = nota.Id;

        string S(string? t, int max) { var s = _sanitizador.Sanitizar(t ?? "").Texto.Trim(); return s.Length > max ? s[..max] : s; }

        var c = d.Contexto;
        var contexto = JsonSerializer.Serialize(new
        {
            versao = Versao,
            tela = c?.Tela,
            operacao = c?.Op,
            foco = c?.Foco?.Campo,
            erros_api = c?.Api?.Select(e => new { e.Rota, e.Status, e.Codigo, mensagem = S(e.Mensagem, 300) }),
            erros_campos = c?.Erros?.Select(e => new { e.Campo, mensagem = S(e.Mensagem, 300) }),
            rastro = c?.Rastro?.TakeLast(15).Select(x => S(x, 200)),
            sentry_event_id = c?.SentryEventId
        });

        var titulo = S(d.Titulo, 200);
        var chamado = Chamado.Criar(r.EscritorioId, r.EmpresaId, r.UsuarioId,
            string.IsNullOrWhiteSpace(titulo) ? "Problema relatado à Ori" : titulo,
            string.IsNullOrWhiteSpace(d.Descricao) ? "(sem descrição)" : S(d.Descricao, 3900),
            string.IsNullOrWhiteSpace(d.Passos) ? null : S(d.Passos, 3900),
            d.Severidade, notaId, contexto.Length > 15000 ? contexto[..15000] : contexto);
        await _chamados.AddAsync(chamado, ct);
        await _uow.SaveChangesAsync(ct);
        return chamado.Id;
    }
}

public class TriagemChamadosHandlers :
    IRequestHandler<ListarChamadosQuery, List<ChamadoInternoDto>>,
    IRequestHandler<AtualizarChamadoCommand, bool>
{
    private readonly IChamadoRepository _chamados;
    private readonly IUnitOfWork _uow;
    public TriagemChamadosHandlers(IChamadoRepository chamados, IUnitOfWork uow) { _chamados = chamados; _uow = uow; }

    public async Task<List<ChamadoInternoDto>> Handle(ListarChamadosQuery r, CancellationToken ct) =>
        (await _chamados.ListarAsync(r.Status, Math.Max(1, r.Pagina), Math.Clamp(r.Tamanho, 1, 100), ct))
            .Select(c => new ChamadoInternoDto(c.Id, c.Titulo, c.Descricao, c.Passos, c.Severidade, c.Status, c.EscritorioId,
                c.EmpresaId, c.NotaFiscalId, c.ContextoTecnicoJson, c.IssueUrl, c.NotaTriagem, c.CreatedAt))
            .ToList();

    public async Task<bool> Handle(AtualizarChamadoCommand r, CancellationToken ct)
    {
        var c = await _chamados.GetByIdAsync(r.Id, ct);
        if (c == null) return false;
        c.AtualizarTriagem(r.Dto.Status, r.Dto.IssueUrl, r.Dto.Nota);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
