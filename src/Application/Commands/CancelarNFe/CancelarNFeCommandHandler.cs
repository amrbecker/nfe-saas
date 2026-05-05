using MediatR;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace NfeSaas.Application.Commands.CancelarNFe;

public record CancelarNFeCommand(Guid NotaFiscalId, Guid EmpresaId, string Justificativa) : IRequest<CancelarNFeResult>;
public record CancelarNFeResult(bool Sucesso, string? MensagemErro);

public class CancelarNFeCommandHandler : IRequestHandler<CancelarNFeCommand, CancelarNFeResult>
{
    private readonly INotaFiscalRepository _notaRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly ISefazService _sefaz;
    private readonly IXmlNFeService _xmlService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CancelarNFeCommandHandler> _logger;

    public CancelarNFeCommandHandler(
        INotaFiscalRepository notaRepo, IEmpresaRepository empresaRepo,
        ISefazService sefaz, IXmlNFeService xmlService,
        IUnitOfWork uow, ILogger<CancelarNFeCommandHandler> logger)
    {
        _notaRepo = notaRepo;
        _empresaRepo = empresaRepo;
        _sefaz = sefaz;
        _xmlService = xmlService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<CancelarNFeResult> Handle(CancelarNFeCommand request, CancellationToken cancellationToken)
    {
        var nota = await _notaRepo.GetByIdAsync(request.NotaFiscalId, cancellationToken);
        if (nota == null) return new CancelarNFeResult(false, "Nota fiscal não encontrada.");
        if (nota.EmpresaId != request.EmpresaId) return new CancelarNFeResult(false, "Acesso negado.");
        if (nota.Situacao != NfeSaas.Domain.Enums.SituacaoNota.Autorizada)
            return new CancelarNFeResult(false, "Apenas notas autorizadas podem ser canceladas.");

        if (nota.DataAutorizacao.HasValue && (DateTime.UtcNow - nota.DataAutorizacao.Value).TotalHours > 24)
            return new CancelarNFeResult(false, "Prazo para cancelamento expirado (24 horas).");

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
        if (empresa == null) return new CancelarNFeResult(false, "Empresa não encontrada.");

        var resultado = await _sefaz.CancelarNFeAsync(nota, empresa, request.Justificativa, cancellationToken);

        if (resultado.Sucesso)
        {
            var xmlCanc = _xmlService.GerarXmlCancelamento(nota.ChaveAcesso!, request.Justificativa, empresa);
            nota.Cancelar(xmlCanc);
            await _notaRepo.UpdateAsync(nota, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("NF-e {Chave} cancelada com sucesso.", nota.ChaveAcesso);
        }

        return new CancelarNFeResult(resultado.Sucesso, resultado.MensagemErro);
    }
}
