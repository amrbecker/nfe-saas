using MediatR;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.RetransmitirNFe;

// UsuarioId é opcional (default) para não quebrar chamadores/testes existentes que ainda
// não repassam o usuário autenticado — quando ausente (Guid.Empty), a auditoria não associa
// um usuário específico ao evento (mesma convenção de CancelarNFeCommand).
public record RetransmitirNFeCommand(Guid NotaFiscalId, Guid EmpresaId, Guid UsuarioId = default)
    : IRequest<RetransmitirNFeResult>;

public record RetransmitirNFeResult(bool Sucesso, string? ChaveAcesso, string? Protocolo, string? MensagemErro);

public class RetransmitirNFeCommandHandler : IRequestHandler<RetransmitirNFeCommand, RetransmitirNFeResult>
{
    private readonly INotaFiscalRepository _notaRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly ISefazService _sefaz;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RetransmitirNFeCommandHandler> _logger;

    public RetransmitirNFeCommandHandler(
        INotaFiscalRepository notaRepo, IEmpresaRepository empresaRepo,
        ISefazService sefaz, IAuditService auditService, IUnitOfWork uow,
        ILogger<RetransmitirNFeCommandHandler> logger)
    {
        _notaRepo = notaRepo;
        _empresaRepo = empresaRepo;
        _sefaz = sefaz;
        _auditService = auditService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<RetransmitirNFeResult> Handle(RetransmitirNFeCommand request, CancellationToken cancellationToken)
    {
        var nota = await _notaRepo.GetByIdAsync(request.NotaFiscalId, cancellationToken);
        if (nota == null) return new RetransmitirNFeResult(false, null, null, "Nota fiscal não encontrada.");
        if (nota.EmpresaId != request.EmpresaId) return new RetransmitirNFeResult(false, null, null, "Acesso negado.");
        if (nota.Situacao != SituacaoNota.PendenteRetransmissao)
            return new RetransmitirNFeResult(false, null, null, "Esta nota não está pendente de retransmissão.");
        if (string.IsNullOrEmpty(nota.XmlEnvio))
            return new RetransmitirNFeResult(false, null, null, "Nota sem XML assinado — não é possível retransmitir.");

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
        if (empresa == null) return new RetransmitirNFeResult(false, null, null, "Empresa não encontrada.");

        // Reenvia o MESMO XmlEnvio já assinado — mesma chave de acesso/número, sem consumir
        // nova numeração (diferente de emitir uma nota nova).
        var resultado = await _sefaz.EnviarNFeAsync(nota, empresa, cancellationToken);
        var semAcessoSefaz = !resultado.Sucesso && resultado.CodigoRetorno == -1;

        if (resultado.Sucesso)
        {
            nota.Autorizar(resultado.ChaveAcesso!, resultado.Protocolo!, resultado.XmlRetorno!);
            _logger.LogInformation("NF-e {Numero} autorizada na retransmissão. Chave: {Chave}", nota.Numero, resultado.ChaveAcesso);
        }
        else if (semAcessoSefaz)
        {
            nota.MarcarPendenteRetransmissao(resultado.MensagemErro ?? "SEFAZ indisponível.");
            _logger.LogWarning("NF-e {Numero} segue pendente de retransmissão — SEFAZ ainda inacessível.", nota.Numero);
        }
        else
        {
            nota.Rejeitar(resultado.MensagemErro ?? "Erro desconhecido");
            _logger.LogWarning("NF-e {Numero} rejeitada na retransmissão: {Motivo}", nota.Numero, resultado.MensagemErro);
        }

        await _notaRepo.UpdateAsync(nota, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var usuarioAuditoria = request.UsuarioId == Guid.Empty ? (Guid?)null : request.UsuarioId;
        var auditDetalhes = resultado.Sucesso
            ? $"Autorizada. Protocolo: {resultado.Protocolo}"
            : $"{(semAcessoSefaz ? "Pendente retransmissão" : "Rejeitada")}: {resultado.MensagemErro}";
        await _auditService.RegistrarAsync(empresa.Id,
            resultado.Sucesso ? "NFe.Autorizada" : (semAcessoSefaz ? "NFe.PendenteRetransmissao" : "NFe.Rejeitada"),
            usuarioAuditoria, nota.ChaveAcesso ?? resultado.ChaveAcesso, auditDetalhes,
            ct: cancellationToken);

        return new RetransmitirNFeResult(resultado.Sucesso, resultado.ChaveAcesso, resultado.Protocolo, resultado.MensagemErro);
    }
}
