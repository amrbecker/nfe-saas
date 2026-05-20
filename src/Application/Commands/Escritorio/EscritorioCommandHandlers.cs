using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Commands.EscritorioCommands;

// === CRIAR ESCRITÓRIO (auto-cadastro) ===
public record CreateEscritorioCommand(CreateEscritorioDto Dto) : IRequest<EscritorioDto?>;

public class CreateEscritorioCommandHandler : IRequestHandler<CreateEscritorioCommand, EscritorioDto?>
{
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IUnitOfWork _uow;

    public CreateEscritorioCommandHandler(IEscritorioRepository escritorioRepo, IUsuarioRepository usuarioRepo, IUnitOfWork uow)
    {
        _escritorioRepo = escritorioRepo;
        _usuarioRepo = usuarioRepo;
        _uow = uow;
    }

    public async Task<EscritorioDto?> Handle(CreateEscritorioCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        if (!CnpjValidator.Validar(dto.Cnpj)) return null;

        var existing = await _escritorioRepo.GetByCnpjAsync(CnpjValidator.ApenasDigitos(dto.Cnpj), cancellationToken);
        if (existing != null) return null;

        if (!Enum.IsDefined(typeof(PlanoSaas), dto.Plano))
            return null; // Plano Free não existe — escolha obrigatória entre Basico/Profissional/Enterprise
        var plano = (PlanoSaas)dto.Plano;
        var escritorio = Escritorio.Criar(dto.RazaoSocial, dto.NomeFantasia, dto.Cnpj, dto.Email, dto.Telefone, plano);
        await _escritorioRepo.AddAsync(escritorio, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.SenhaAdmin);
        var admin = Usuario.Criar(escritorio.Id, dto.NomeAdmin, dto.EmailAdmin, senhaHash, "Admin");
        await _usuarioRepo.AddAsync(admin, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EscritorioDto(escritorio.Id, escritorio.RazaoSocial, escritorio.NomeFantasia, escritorio.Cnpj,
            escritorio.Email, escritorio.Telefone, escritorio.Plano.ToString(), escritorio.Ativo);
    }
}

// === CRIAR EMPRESA NO ESCRITÓRIO ===
public record CreateEmpresaCommand(Guid EscritorioId, CreateEmpresaDto Dto) : IRequest<EmpresaResumoDto?>;

public class CreateEmpresaCommandHandler : IRequestHandler<CreateEmpresaCommand, EmpresaResumoDto?>
{
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly IUnitOfWork _uow;

    public CreateEmpresaCommandHandler(IEmpresaRepository empresaRepo, IEscritorioRepository escritorioRepo, IUnitOfWork uow)
    {
        _empresaRepo = empresaRepo;
        _escritorioRepo = escritorioRepo;
        _uow = uow;
    }

    public async Task<EmpresaResumoDto?> Handle(CreateEmpresaCommand request, CancellationToken cancellationToken)
    {
        var escritorio = await _escritorioRepo.GetByIdAsync(request.EscritorioId, cancellationToken);
        if (escritorio == null) return null;

        var dto = request.Dto;

        if (!CnpjValidator.Validar(dto.Cnpj)) return null;
        if (!IeValidator.UfValida(dto.Uf)) return null;
        if (!IeValidator.Validar(dto.InscricaoEstadual, dto.Uf)) return null;
        if (dto.Cep.Where(char.IsDigit).Count() != 8) return null;
        if (!string.IsNullOrWhiteSpace(dto.Cnae) && !CnaeValidator.Validar(dto.Cnae)) return null;

        var empresa = Empresa.Criar(
            request.EscritorioId,
            dto.RazaoSocial, dto.NomeFantasia, dto.Cnpj, dto.InscricaoEstadual,
            dto.Logradouro, dto.Numero, dto.Bairro, dto.Cidade, dto.Uf,
            dto.Cep, dto.CodigoMunicipio, dto.Telefone, dto.Email,
            (RegimeTributario)dto.RegimeTributario, (AmbienteSefaz)dto.AmbienteSefaz,
            string.IsNullOrWhiteSpace(dto.Cnae) ? null : dto.Cnae.Trim());

        await _empresaRepo.AddAsync(empresa, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EmpresaResumoDto(empresa.Id, empresa.RazaoSocial, empresa.NomeFantasia, empresa.Cnpj);
    }
}

// === ATIVAR PLANO PAGO ===
// Em produção, deve ser chamado pelo webhook do gateway de pagamento. Aqui é um endpoint
// administrativo simples (Role=Admin do próprio escritório ou super-admin).
public record AtivarPlanoPagoCommand(Guid EscritorioId, DateTime AtivoAteUtc, decimal? ValorPago) : IRequest<bool>;

public class AtivarPlanoPagoCommandHandler : IRequestHandler<AtivarPlanoPagoCommand, bool>
{
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly IUnitOfWork _uow;

    public AtivarPlanoPagoCommandHandler(IEscritorioRepository escritorioRepo, IUnitOfWork uow)
    {
        _escritorioRepo = escritorioRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(AtivarPlanoPagoCommand request, CancellationToken ct)
    {
        var escritorio = await _escritorioRepo.GetByIdAsync(request.EscritorioId, ct);
        if (escritorio == null) return false;
        if (request.AtivoAteUtc <= DateTime.UtcNow) return false;
        escritorio.AtivarPlanoPago(request.AtivoAteUtc);
        await _escritorioRepo.UpdateAsync(escritorio, ct);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}

// === CADASTRAR O PRÓPRIO ESCRITÓRIO COMO EMPRESA EMITENTE ===
// Permite que o escritório (que é PJ com CNPJ) emita NF-e em seu próprio nome,
// sem precisar criar uma "empresa cliente" duplicando os dados.
public record CadastrarEscritorioComoEmpresaCommand(
    Guid EscritorioId,
    string InscricaoEstadual,
    string Logradouro, string Numero, string Bairro, string Cidade, string Uf,
    string Cep, string CodigoMunicipio,
    int RegimeTributario, int AmbienteSefaz,
    string? Cnae) : IRequest<EmpresaResumoDto?>;

public class CadastrarEscritorioComoEmpresaCommandHandler : IRequestHandler<CadastrarEscritorioComoEmpresaCommand, EmpresaResumoDto?>
{
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IUnitOfWork _uow;

    public CadastrarEscritorioComoEmpresaCommandHandler(
        IEscritorioRepository escritorioRepo, IEmpresaRepository empresaRepo, IUnitOfWork uow)
    {
        _escritorioRepo = escritorioRepo;
        _empresaRepo = empresaRepo;
        _uow = uow;
    }

    public async Task<EmpresaResumoDto?> Handle(CadastrarEscritorioComoEmpresaCommand request, CancellationToken ct)
    {
        var escritorio = await _escritorioRepo.GetByIdAsync(request.EscritorioId, ct);
        if (escritorio == null) return null;

        // Validações dos campos fiscais (CNPJ já foi validado no cadastro do escritório).
        if (!IeValidator.UfValida(request.Uf)) return null;
        if (!IeValidator.Validar(request.InscricaoEstadual, request.Uf)) return null;
        if (request.Cep.Where(char.IsDigit).Count() != 8) return null;
        if (!string.IsNullOrWhiteSpace(request.Cnae) && !CnaeValidator.Validar(request.Cnae)) return null;
        if (!Enum.IsDefined(typeof(RegimeTributario), request.RegimeTributario)) return null;
        if (!Enum.IsDefined(typeof(AmbienteSefaz), request.AmbienteSefaz)) return null;

        // Idempotência: se já existe Empresa com este CNPJ no escritório, retorna a existente.
        var cnpjDigitos = CnpjValidator.ApenasDigitos(escritorio.Cnpj);
        var existente = await _empresaRepo.GetByCnpjAsync(cnpjDigitos, ct);
        if (existente != null && existente.EscritorioId == escritorio.Id)
            return new EmpresaResumoDto(existente.Id, existente.RazaoSocial, existente.NomeFantasia, existente.Cnpj);
        if (existente != null && existente.EscritorioId != escritorio.Id)
            return null; // CNPJ já cadastrado em outro escritório

        var empresa = Empresa.Criar(
            escritorio.Id,
            escritorio.RazaoSocial,
            escritorio.NomeFantasia,
            cnpjDigitos,
            request.InscricaoEstadual,
            request.Logradouro, request.Numero, request.Bairro, request.Cidade, request.Uf,
            request.Cep, request.CodigoMunicipio,
            escritorio.Telefone ?? "",
            escritorio.Email,
            (RegimeTributario)request.RegimeTributario,
            (AmbienteSefaz)request.AmbienteSefaz,
            string.IsNullOrWhiteSpace(request.Cnae) ? null : request.Cnae.Trim());

        await _empresaRepo.AddAsync(empresa, ct);
        await _uow.SaveChangesAsync(ct);
        return new EmpresaResumoDto(empresa.Id, empresa.RazaoSocial, empresa.NomeFantasia, empresa.Cnpj);
    }
}

// === CRIAR USUÁRIO NO ESCRITÓRIO ===
public record CreateUsuarioCommand(Guid EscritorioId, CreateUsuarioDto Dto) : IRequest<UsuarioResumoDto?>;

public class CreateUsuarioCommandHandler : IRequestHandler<CreateUsuarioCommand, UsuarioResumoDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEscritorioRepository _escritorioRepo;
    private readonly IUnitOfWork _uow;

    public CreateUsuarioCommandHandler(IUsuarioRepository usuarioRepo, IEscritorioRepository escritorioRepo, IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _escritorioRepo = escritorioRepo;
        _uow = uow;
    }

    public async Task<UsuarioResumoDto?> Handle(CreateUsuarioCommand request, CancellationToken cancellationToken)
    {
        var escritorio = await _escritorioRepo.GetByIdAsync(request.EscritorioId, cancellationToken);
        if (escritorio == null) return null;

        var existing = await _usuarioRepo.GetByEmailAsync(request.Dto.Email, cancellationToken);
        if (existing != null) return null;

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(request.Dto.Senha);
        var usuario = Usuario.Criar(request.EscritorioId, request.Dto.Nome, request.Dto.Email, senhaHash, request.Dto.Role);

        await _usuarioRepo.AddAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new UsuarioResumoDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Role, usuario.Ativo);
    }
}

// === ATUALIZAR USUÁRIO ===
public record UpdateUsuarioCommand(Guid EscritorioId, Guid UsuarioId, UpdateUsuarioDto Dto) : IRequest<UsuarioResumoDto?>;

public class UpdateUsuarioCommandHandler : IRequestHandler<UpdateUsuarioCommand, UsuarioResumoDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IUnitOfWork _uow;

    public UpdateUsuarioCommandHandler(IUsuarioRepository usuarioRepo, IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _uow = uow;
    }

    public async Task<UsuarioResumoDto?> Handle(UpdateUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByIdAsync(request.UsuarioId, cancellationToken);
        if (usuario == null || usuario.EscritorioId != request.EscritorioId || usuario.IsDeleted)
            return null;

        usuario.Atualizar(request.Dto.Nome, request.Dto.Role);
        await _usuarioRepo.UpdateAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new UsuarioResumoDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Role, usuario.Ativo);
    }
}

// === TOGGLE ATIVO USUÁRIO ===
public record ToggleAtivoUsuarioCommand(Guid EscritorioId, Guid UsuarioId) : IRequest<UsuarioResumoDto?>;

public class ToggleAtivoUsuarioCommandHandler : IRequestHandler<ToggleAtivoUsuarioCommand, UsuarioResumoDto?>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IUnitOfWork _uow;

    public ToggleAtivoUsuarioCommandHandler(IUsuarioRepository usuarioRepo, IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _uow = uow;
    }

    public async Task<UsuarioResumoDto?> Handle(ToggleAtivoUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByIdAsync(request.UsuarioId, cancellationToken);
        if (usuario == null || usuario.EscritorioId != request.EscritorioId || usuario.IsDeleted)
            return null;

        if (usuario.Ativo) usuario.Desativar(); else usuario.Ativar();
        await _usuarioRepo.UpdateAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new UsuarioResumoDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Role, usuario.Ativo);
    }
}

// === EXCLUIR USUÁRIO (soft delete) ===
public record DeleteUsuarioCommand(Guid EscritorioId, Guid UsuarioId) : IRequest<bool>;

public class DeleteUsuarioCommandHandler : IRequestHandler<DeleteUsuarioCommand, bool>
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IUnitOfWork _uow;

    public DeleteUsuarioCommandHandler(IUsuarioRepository usuarioRepo, IUnitOfWork uow)
    {
        _usuarioRepo = usuarioRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepo.GetByIdAsync(request.UsuarioId, cancellationToken);
        if (usuario == null || usuario.EscritorioId != request.EscritorioId || usuario.IsDeleted)
            return false;

        usuario.Delete();
        await _usuarioRepo.UpdateAsync(usuario, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
