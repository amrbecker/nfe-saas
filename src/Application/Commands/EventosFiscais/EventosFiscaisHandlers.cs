using MediatR;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.DTOs;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.EventosFiscaisCommands;

public record EventoFiscalResult(EventoFiscalResumoDto? Evento, string? Erro);

// =========================================================
// CC-e — Carta de Correção
// =========================================================
public record EmitirCartaCorrecaoCommand(Guid EmpresaId, Guid UsuarioId, Guid NotaFiscalId, string Correcao)
    : IRequest<EventoFiscalResult>;

public class EmitirCartaCorrecaoHandler : IRequestHandler<EmitirCartaCorrecaoCommand, EventoFiscalResult>
{
    private readonly INotaFiscalRepository _notaRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEventoFiscalRepository _eventoRepo;
    private readonly ISefazService _sefaz;
    private readonly IXmlNFeService _xml;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<EmitirCartaCorrecaoHandler> _logger;

    public EmitirCartaCorrecaoHandler(
        INotaFiscalRepository notaRepo, IEmpresaRepository empresaRepo,
        IEventoFiscalRepository eventoRepo, ISefazService sefaz, IXmlNFeService xml,
        IUnitOfWork uow, IAuditService audit, ILogger<EmitirCartaCorrecaoHandler> logger)
    {
        _notaRepo = notaRepo; _empresaRepo = empresaRepo; _eventoRepo = eventoRepo;
        _sefaz = sefaz; _xml = xml; _uow = uow; _audit = audit; _logger = logger;
    }

    public async Task<EventoFiscalResult> Handle(EmitirCartaCorrecaoCommand request, CancellationToken ct)
    {
        var correcao = (request.Correcao ?? "").Trim();
        if (correcao.Length < 15)
            return new EventoFiscalResult(null, "Texto da correção deve ter no mínimo 15 caracteres.");
        if (correcao.Length > 1000)
            return new EventoFiscalResult(null, "Texto da correção deve ter no máximo 1000 caracteres.");

        var nota = await _notaRepo.GetByIdAsync(request.NotaFiscalId, ct);
        if (nota == null || nota.EmpresaId != request.EmpresaId)
            return new EventoFiscalResult(null, "Nota fiscal não encontrada.");
        if (nota.Situacao != SituacaoNota.Autorizada)
            return new EventoFiscalResult(null, "Apenas notas autorizadas aceitam Carta de Correção.");
        if (string.IsNullOrEmpty(nota.ChaveAcesso))
            return new EventoFiscalResult(null, "Nota não possui chave de acesso.");

        // Prazo: até 30 dias após autorização
        if (nota.DataAutorizacao.HasValue && (DateTime.UtcNow - nota.DataAutorizacao.Value).TotalDays > 30)
            return new EventoFiscalResult(null, "Prazo legal para Carta de Correção expirado (30 dias após autorização).");

        // Limite: até 20 CC-e aceitas por NF-e
        var qtdAceitas = await _eventoRepo.CountCcePorChaveAsync(request.EmpresaId, nota.ChaveAcesso, ct);
        if (qtdAceitas >= 20)
            return new EventoFiscalResult(null, "Limite de 20 Cartas de Correção por NF-e atingido.");

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, ct);
        if (empresa == null) return new EventoFiscalResult(null, "Empresa não encontrada.");
        if (!empresa.CertificadoValido())
            return new EventoFiscalResult(null, "Certificado digital inválido ou não configurado — eventos fiscais exigem assinatura.");

        var sequencial = qtdAceitas + 1;
        var evento = EventoFiscal.CriarCce(empresa.Id, request.UsuarioId, empresa.AmbienteSefaz,
            nota.ChaveAcesso, sequencial, correcao);

        var xmlEvento = _xml.GerarXmlCce(nota.ChaveAcesso, sequencial, correcao, empresa);
        var xmlAssinado = _xml.AssinarEvento(xmlEvento, empresa.CertificadoBytes!, empresa.CertificadoSenha!);
        evento.RegistrarEnvio(xmlAssinado);

        var resultado = await _sefaz.EnviarEventoCceAsync(evento, empresa, ct);
        if (resultado.Sucesso) evento.Aceitar(resultado.Protocolo!, resultado.XmlRetorno);
        else evento.Rejeitar(resultado.MensagemErro ?? "Erro desconhecido", resultado.XmlRetorno);

        await _eventoRepo.AddAsync(evento, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(empresa.Id,
            resultado.Sucesso ? "CCe.Aceita" : "CCe.Rejeitada",
            request.UsuarioId, nota.ChaveAcesso,
            resultado.Sucesso ? $"Sequencial {sequencial}. Protocolo: {resultado.Protocolo}"
                              : $"Rejeitada: {resultado.MensagemErro}",
            ct: ct);

        if (!resultado.Sucesso)
            return new EventoFiscalResult(MapResumo(evento), resultado.MensagemErro);

        _logger.LogInformation("CC-e #{Seq} aceita para chave {Chave}", sequencial, nota.ChaveAcesso);
        return new EventoFiscalResult(MapResumo(evento), null);
    }

    internal static EventoFiscalResumoDto MapResumo(EventoFiscal e) => new(
        e.Id, (int)e.Tipo, e.ChaveAcesso, e.SequencialCce,
        e.AnoInutilizacao, (int?)e.TipoNotaInutilizacao,
        e.SerieInutilizacao, e.NumeroInicialInutilizacao, e.NumeroFinalInutilizacao,
        e.Justificativa, (int)e.Situacao, e.Protocolo, e.MotivoRejeicao,
        e.DataEvento, e.DataRetorno);
}

// =========================================================
// Inutilização de numeração
// =========================================================
public record InutilizarNumeracaoCommand(Guid EmpresaId, Guid UsuarioId, InutilizarDto Dto)
    : IRequest<EventoFiscalResult>;

public class InutilizarNumeracaoHandler : IRequestHandler<InutilizarNumeracaoCommand, EventoFiscalResult>
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEventoFiscalRepository _eventoRepo;
    private readonly INotaFiscalRepository _notaRepo;
    private readonly ISefazService _sefaz;
    private readonly IXmlNFeService _xml;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<InutilizarNumeracaoHandler> _logger;

    public InutilizarNumeracaoHandler(
        IEmpresaRepository empresaRepo, IEventoFiscalRepository eventoRepo,
        INotaFiscalRepository notaRepo, ISefazService sefaz, IXmlNFeService xml,
        IUnitOfWork uow, IAuditService audit, ILogger<InutilizarNumeracaoHandler> logger)
    {
        _empresaRepo = empresaRepo; _eventoRepo = eventoRepo; _notaRepo = notaRepo;
        _sefaz = sefaz; _xml = xml; _uow = uow; _audit = audit; _logger = logger;
    }

    public async Task<EventoFiscalResult> Handle(InutilizarNumeracaoCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Validações
        var justificativa = (dto.Justificativa ?? "").Trim();
        if (justificativa.Length < 15)
            return new EventoFiscalResult(null, "Justificativa deve ter no mínimo 15 caracteres (exigência SEFAZ).");
        if (justificativa.Length > 255)
            return new EventoFiscalResult(null, "Justificativa deve ter no máximo 255 caracteres.");
        if (dto.NumeroInicial < 1 || dto.NumeroFinal < dto.NumeroInicial)
            return new EventoFiscalResult(null, "Números inicial/final inválidos.");
        if (dto.Ano < 2000 || dto.Ano > DateTime.UtcNow.Year)
            return new EventoFiscalResult(null, "Ano inválido para inutilização.");
        if (dto.TipoNota != (int)TipoNota.NFe && dto.TipoNota != (int)TipoNota.NFCe)
            return new EventoFiscalResult(null, "Tipo de nota inválido (use 55 ou 65).");
        if (dto.Serie < 0 || dto.Serie > 999)
            return new EventoFiscalResult(null, "Série inválida.");

        var tipo = (TipoNota)dto.TipoNota;
        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, ct);
        if (empresa == null) return new EventoFiscalResult(null, "Empresa não encontrada.");
        if (!empresa.CertificadoValido())
            return new EventoFiscalResult(null, "Certificado digital inválido ou não configurado — inutilização exige assinatura.");

        // Não pode inutilizar número que já foi usado em nota emitida
        for (var n = dto.NumeroInicial; n <= dto.NumeroFinal; n++)
        {
            var existente = await _notaRepo.GetBySerieNumeroAsync(empresa.Id, tipo, dto.Serie, n, empresa.AmbienteSefaz, ct);
            if (existente != null)
                return new EventoFiscalResult(null,
                    $"Número {n} (série {dto.Serie}) já foi usado em nota fiscal — não pode ser inutilizado.");
        }

        // Bloqueia overlap com inutilização anterior aceita
        var conflito = await _eventoRepo.GetInutilizacaoConflitoAsync(
            empresa.Id, empresa.AmbienteSefaz, dto.Ano, tipo, dto.Serie, dto.NumeroInicial, dto.NumeroFinal, ct);
        if (conflito != null)
            return new EventoFiscalResult(null,
                $"Range conflita com inutilização já registrada ({conflito.NumeroInicialInutilizacao}-{conflito.NumeroFinalInutilizacao}).");

        var evento = EventoFiscal.CriarInutilizacao(empresa.Id, request.UsuarioId, empresa.AmbienteSefaz,
            dto.Ano, tipo, dto.Serie, dto.NumeroInicial, dto.NumeroFinal, justificativa);

        var xmlEvento = _xml.GerarXmlInutilizacao(empresa, dto.Ano, tipo, dto.Serie, dto.NumeroInicial, dto.NumeroFinal, justificativa);
        var xmlAssinado = _xml.AssinarInutilizacao(xmlEvento, empresa.CertificadoBytes!, empresa.CertificadoSenha!);
        evento.RegistrarEnvio(xmlAssinado);

        var resultado = await _sefaz.EnviarInutilizacaoAsync(evento, empresa, ct);
        if (resultado.Sucesso) evento.Aceitar(resultado.Protocolo!, resultado.XmlRetorno);
        else evento.Rejeitar(resultado.MensagemErro ?? "Erro desconhecido", resultado.XmlRetorno);

        await _eventoRepo.AddAsync(evento, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(empresa.Id,
            resultado.Sucesso ? "Inutilizacao.Aceita" : "Inutilizacao.Rejeitada",
            request.UsuarioId, null,
            $"Série {dto.Serie} {dto.NumeroInicial}-{dto.NumeroFinal} ({dto.Ano}). " +
            (resultado.Sucesso ? $"Protocolo: {resultado.Protocolo}" : $"Erro: {resultado.MensagemErro}"),
            ct: ct);

        if (!resultado.Sucesso)
            return new EventoFiscalResult(EmitirCartaCorrecaoHandler.MapResumo(evento), resultado.MensagemErro);

        _logger.LogInformation("Inutilização aceita: série {Serie} {Ini}-{Fin}",
            dto.Serie, dto.NumeroInicial, dto.NumeroFinal);
        return new EventoFiscalResult(EmitirCartaCorrecaoHandler.MapResumo(evento), null);
    }
}

// =========================================================
// Manifestação do Destinatário
// =========================================================
public record ManifestarDestinatarioCommand(Guid EmpresaId, Guid UsuarioId, string ChaveAcesso, ManifestarDto Dto)
    : IRequest<EventoFiscalResult>;

public class ManifestarDestinatarioHandler : IRequestHandler<ManifestarDestinatarioCommand, EventoFiscalResult>
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEventoFiscalRepository _eventoRepo;
    private readonly ISefazService _sefaz;
    private readonly IXmlNFeService _xml;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<ManifestarDestinatarioHandler> _logger;

    public ManifestarDestinatarioHandler(
        IEmpresaRepository empresaRepo, IEventoFiscalRepository eventoRepo,
        ISefazService sefaz, IXmlNFeService xml,
        IUnitOfWork uow, IAuditService audit, ILogger<ManifestarDestinatarioHandler> logger)
    {
        _empresaRepo = empresaRepo; _eventoRepo = eventoRepo;
        _sefaz = sefaz; _xml = xml; _uow = uow; _audit = audit; _logger = logger;
    }

    public async Task<EventoFiscalResult> Handle(ManifestarDestinatarioCommand request, CancellationToken ct)
    {
        var chave = (request.ChaveAcesso ?? "").Trim();
        if (chave.Length != 44 || !chave.All(char.IsDigit))
            return new EventoFiscalResult(null, "Chave de acesso deve ter 44 dígitos.");

        if (!Enum.IsDefined(typeof(TipoEventoFiscal), request.Dto.Tipo))
            return new EventoFiscalResult(null, "Tipo de manifestação inválido.");

        var tipo = (TipoEventoFiscal)request.Dto.Tipo;
        if (tipo is not (TipoEventoFiscal.ManifestacaoConfirmacao
                      or TipoEventoFiscal.ManifestacaoCiencia
                      or TipoEventoFiscal.ManifestacaoDesconhecimento
                      or TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada))
            return new EventoFiscalResult(null, "Tipo de manifestação inválido.");

        var justificativa = (request.Dto.Justificativa ?? "").Trim();
        if (tipo == TipoEventoFiscal.ManifestacaoOperacaoNaoRealizada)
        {
            if (justificativa.Length < 15) return new EventoFiscalResult(null, "Justificativa obrigatória (mín. 15 chars) para 'Operação Não Realizada'.");
            if (justificativa.Length > 255) return new EventoFiscalResult(null, "Justificativa máxima 255 caracteres.");
        }

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, ct);
        if (empresa == null) return new EventoFiscalResult(null, "Empresa não encontrada.");
        if (!empresa.CertificadoValido())
            return new EventoFiscalResult(null, "Certificado digital inválido ou não configurado — manifestação exige assinatura.");

        var evento = EventoFiscal.CriarManifestacao(empresa.Id, request.UsuarioId, empresa.AmbienteSefaz,
            tipo, chave, justificativa);

        var xmlEvento = _xml.GerarXmlManifestacao(chave, tipo, justificativa, empresa);
        var xmlAssinado = _xml.AssinarEvento(xmlEvento, empresa.CertificadoBytes!, empresa.CertificadoSenha!);
        evento.RegistrarEnvio(xmlAssinado);

        var resultado = await _sefaz.EnviarManifestacaoAsync(evento, empresa, ct);
        if (resultado.Sucesso) evento.Aceitar(resultado.Protocolo!, resultado.XmlRetorno);
        else evento.Rejeitar(resultado.MensagemErro ?? "Erro desconhecido", resultado.XmlRetorno);

        await _eventoRepo.AddAsync(evento, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.RegistrarAsync(empresa.Id,
            resultado.Sucesso ? $"Manifestacao.{tipo}.Aceita" : $"Manifestacao.{tipo}.Rejeitada",
            request.UsuarioId, chave,
            resultado.Sucesso ? $"Protocolo: {resultado.Protocolo}" : $"Erro: {resultado.MensagemErro}",
            ct: ct);

        if (!resultado.Sucesso)
            return new EventoFiscalResult(EmitirCartaCorrecaoHandler.MapResumo(evento), resultado.MensagemErro);

        _logger.LogInformation("Manifestação aceita: {Tipo} chave {Chave}", tipo, chave);
        return new EventoFiscalResult(EmitirCartaCorrecaoHandler.MapResumo(evento), null);
    }
}

// =========================================================
// Queries
// =========================================================
public record GetEventosPorChaveQuery(Guid EmpresaId, string ChaveAcesso) : IRequest<List<EventoFiscalResumoDto>>;

public class GetEventosPorChaveHandler : IRequestHandler<GetEventosPorChaveQuery, List<EventoFiscalResumoDto>>
{
    private readonly IEventoFiscalRepository _repo;
    public GetEventosPorChaveHandler(IEventoFiscalRepository repo) => _repo = repo;

    public async Task<List<EventoFiscalResumoDto>> Handle(GetEventosPorChaveQuery request, CancellationToken ct)
    {
        var eventos = await _repo.GetByChaveAcessoAsync(request.EmpresaId, request.ChaveAcesso, ct);
        return eventos.Select(EmitirCartaCorrecaoHandler.MapResumo).ToList();
    }
}

public record GetInutilizacoesQuery(Guid EmpresaId, AmbienteSefaz Ambiente) : IRequest<List<EventoFiscalResumoDto>>;

public class GetInutilizacoesHandler : IRequestHandler<GetInutilizacoesQuery, List<EventoFiscalResumoDto>>
{
    private readonly IEventoFiscalRepository _repo;
    public GetInutilizacoesHandler(IEventoFiscalRepository repo) => _repo = repo;

    public async Task<List<EventoFiscalResumoDto>> Handle(GetInutilizacoesQuery request, CancellationToken ct)
    {
        var eventos = await _repo.GetInutilizacoesAsync(request.EmpresaId, request.Ambiente, ct);
        return eventos.Select(EmitirCartaCorrecaoHandler.MapResumo).ToList();
    }
}
