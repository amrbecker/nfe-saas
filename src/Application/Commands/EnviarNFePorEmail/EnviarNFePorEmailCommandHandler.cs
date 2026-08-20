using System.Net.Mail;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using NfeSaas.Application.Interfaces;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.EnviarNFePorEmail;

public record EnviarNFePorEmailCommand(Guid NotaFiscalId, Guid EmpresaId, Guid UsuarioId, string? EmailDestino)
    : IRequest<EnviarNFePorEmailResult>;

public record EnviarNFePorEmailResult(bool Sucesso, string? MensagemErro);

public class EnviarNFePorEmailCommandHandler : IRequestHandler<EnviarNFePorEmailCommand, EnviarNFePorEmailResult>
{
    private readonly INotaFiscalRepository _notaRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IDanfeService _danfeService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EnviarNFePorEmailCommandHandler> _logger;

    public EnviarNFePorEmailCommandHandler(
        INotaFiscalRepository notaRepo, IEmpresaRepository empresaRepo,
        IDanfeService danfeService, IEmailService emailService, IAuditService auditService,
        IUnitOfWork uow, ILogger<EnviarNFePorEmailCommandHandler> logger)
    {
        _notaRepo = notaRepo;
        _empresaRepo = empresaRepo;
        _danfeService = danfeService;
        _emailService = emailService;
        _auditService = auditService;
        _uow = uow;
        _logger = logger;
    }

    public async Task<EnviarNFePorEmailResult> Handle(EnviarNFePorEmailCommand request, CancellationToken cancellationToken)
    {
        var nota = await _notaRepo.GetByIdAsync(request.NotaFiscalId, cancellationToken);
        if (nota == null) return new EnviarNFePorEmailResult(false, "Nota fiscal não encontrada.");
        if (nota.EmpresaId != request.EmpresaId) return new EnviarNFePorEmailResult(false, "Acesso negado.");
        if (nota.Situacao != SituacaoNota.Autorizada)
            return new EnviarNFePorEmailResult(false, "Apenas notas autorizadas podem ser enviadas por e-mail.");

        var email = (string.IsNullOrWhiteSpace(request.EmailDestino) ? nota.DestinatarioEmail : request.EmailDestino)?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return new EnviarNFePorEmailResult(false, "Nota não possui e-mail do destinatário cadastrado. Informe um e-mail para enviar.");

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            return new EnviarNFePorEmailResult(false, $"E-mail inválido: {email}");
        }

        var empresa = await _empresaRepo.GetByIdAsync(request.EmpresaId, cancellationToken);
        if (empresa == null) return new EnviarNFePorEmailResult(false, "Empresa não encontrada.");

        var danfeBytes = nota.Tipo == TipoNota.NFCe
            ? await _danfeService.GerarDanfeNFCePdfAsync(nota, empresa, cancellationToken)
            : await _danfeService.GerarDanfePdfAsync(nota, empresa, cancellationToken);

        var xmlBytes = Encoding.UTF8.GetBytes(nota.XmlRetorno ?? nota.XmlEnvio!);

        var enviado = await _emailService.EnviarNFeAsync(email, nota.ChaveAcesso!, xmlBytes, danfeBytes, cancellationToken);
        if (!enviado)
            return new EnviarNFePorEmailResult(false, "Falha ao enviar e-mail. Verifique a configuração do Resend ou tente novamente.");

        nota.RegistrarEnvioEmail();
        await _notaRepo.UpdateAsync(nota, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.RegistrarAsync(empresa.Id, "NFe.EmailEnviado", request.UsuarioId,
            nota.ChaveAcesso, $"Enviado para {email}", ct: cancellationToken);

        _logger.LogInformation("NF-e {Chave} enviada por e-mail para {Email}.", nota.ChaveAcesso, email);

        return new EnviarNFePorEmailResult(true, null);
    }
}
