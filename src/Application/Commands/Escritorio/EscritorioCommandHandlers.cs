using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.EscritorioCommands;

// === CRIAR ESCRITÓRIO ===
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
        var existing = await _escritorioRepo.GetByCnpjAsync(dto.Cnpj, cancellationToken);
        if (existing != null) return null;

        var escritorio = Escritorio.Criar(dto.RazaoSocial, dto.NomeFantasia, dto.Cnpj, dto.Email, dto.Telefone);
        await _escritorioRepo.AddAsync(escritorio, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.SenhaAdmin);
        var admin = Usuario.Criar(escritorio.Id, dto.NomeAdmin, dto.EmailAdmin, senhaHash, "Admin");
        await _usuarioRepo.AddAsync(admin, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EscritorioDto(escritorio.Id, escritorio.RazaoSocial, escritorio.NomeFantasia, escritorio.Cnpj, escritorio.Email, escritorio.Telefone, escritorio.Plano.ToString(), escritorio.Ativo);
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
        var empresa = Empresa.Criar(
            request.EscritorioId,
            dto.RazaoSocial, dto.NomeFantasia, dto.Cnpj, dto.InscricaoEstadual,
            dto.Logradouro, dto.Numero, dto.Bairro, dto.Cidade, dto.Uf,
            dto.Cep, dto.CodigoMunicipio, dto.Telefone, dto.Email,
            (RegimeTributario)dto.RegimeTributario, (AmbienteSefaz)dto.AmbienteSefaz);

        await _empresaRepo.AddAsync(empresa, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EmpresaResumoDto(empresa.Id, empresa.RazaoSocial, empresa.NomeFantasia, empresa.Cnpj);
    }
}
