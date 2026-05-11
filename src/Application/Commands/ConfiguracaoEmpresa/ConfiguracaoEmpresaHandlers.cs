using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;

namespace NfeSaas.Application.Commands.ConfiguracaoEmpresaCommands;

// === SALVAR CONFIGURAÇÃO ===
public record SalvarConfiguracaoEmpresaCommand(Guid EmpresaId, ConfiguracaoEmpresaDto Dto) : IRequest<ConfiguracaoEmpresaDto?>;

public class SalvarConfiguracaoEmpresaCommandHandler : IRequestHandler<SalvarConfiguracaoEmpresaCommand, ConfiguracaoEmpresaDto?>
{
    private readonly IConfiguracaoEmpresaRepository _repo;
    private readonly IUnitOfWork _uow;

    public SalvarConfiguracaoEmpresaCommandHandler(IConfiguracaoEmpresaRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ConfiguracaoEmpresaDto?> Handle(SalvarConfiguracaoEmpresaCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var perfil = (PerfilCliente)dto.PerfilCliente;
        var tipoProduto = (TipoProduto)dto.TipoProduto;
        var volume = (VolumeNotas)dto.VolumeNotas;
        var automacao = (NivelAutomacao)dto.NivelAutomacao;
        var relatorio = (NivelRelatorio)dto.NivelRelatorio;

        var existing = await _repo.GetByEmpresaAsync(request.EmpresaId, ct);
        if (existing == null)
        {
            var nova = ConfiguracaoEmpresa.Criar(
                request.EmpresaId, perfil, tipoProduto, volume, automacao,
                dto.EmiteParaConsumidorFinal, dto.OperaIcmsSt, relatorio);
            await _repo.AddAsync(nova, ct);
            await _uow.SaveChangesAsync(ct);
            return ToDto(nova);
        }

        existing.Atualizar(perfil, tipoProduto, volume, automacao,
            dto.EmiteParaConsumidorFinal, dto.OperaIcmsSt, relatorio);
        await _repo.UpdateAsync(existing, ct);
        await _uow.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    static ConfiguracaoEmpresaDto ToDto(ConfiguracaoEmpresa c) => new(
        (int)c.PerfilCliente, (int)c.TipoProduto, (int)c.VolumeNotas, (int)c.NivelAutomacao,
        c.EmiteParaConsumidorFinal, c.OperaIcmsSt, (int)c.NivelRelatorio, c.ConcluidoEm);
}

// === BUSCAR CONFIGURAÇÃO ===
public record GetConfiguracaoEmpresaQuery(Guid EmpresaId) : IRequest<ConfiguracaoEmpresaDto?>;

public class GetConfiguracaoEmpresaQueryHandler : IRequestHandler<GetConfiguracaoEmpresaQuery, ConfiguracaoEmpresaDto?>
{
    private readonly IConfiguracaoEmpresaRepository _repo;

    public GetConfiguracaoEmpresaQueryHandler(IConfiguracaoEmpresaRepository repo) => _repo = repo;

    public async Task<ConfiguracaoEmpresaDto?> Handle(GetConfiguracaoEmpresaQuery request, CancellationToken ct)
    {
        var c = await _repo.GetByEmpresaAsync(request.EmpresaId, ct);
        if (c == null) return null;
        return new ConfiguracaoEmpresaDto(
            (int)c.PerfilCliente, (int)c.TipoProduto, (int)c.VolumeNotas, (int)c.NivelAutomacao,
            c.EmiteParaConsumidorFinal, c.OperaIcmsSt, (int)c.NivelRelatorio, c.ConcluidoEm);
    }
}
