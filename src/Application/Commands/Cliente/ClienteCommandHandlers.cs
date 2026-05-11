using MediatR;
using NfeSaas.Application.DTOs;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Interfaces;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Commands.ClienteCommands;

public record CreateClienteCommand(Guid EmpresaId, CreateClienteDto Dto) : IRequest<ClienteResult>;
public record UpdateClienteCommand(Guid EmpresaId, Guid ClienteId, UpdateClienteDto Dto) : IRequest<ClienteResult>;
public record ToggleAtivoClienteCommand(Guid EmpresaId, Guid ClienteId) : IRequest<ClienteResult>;
public record DeleteClienteCommand(Guid EmpresaId, Guid ClienteId) : IRequest<bool>;

public record ClienteResult(ClienteDetalheDto? Cliente, string? Erro);

public class CreateClienteCommandHandler : IRequestHandler<CreateClienteCommand, ClienteResult>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    public CreateClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ClienteResult> Handle(CreateClienteCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var erro = ValidarCliente(dto.TipoPessoa, dto.CpfCnpj, dto.RazaoSocial, dto.Logradouro,
            dto.Numero, dto.Bairro, dto.Cidade, dto.Uf, dto.Cep, dto.CodigoMunicipio,
            dto.InscricaoEstadual, dto.IndicadorIe);
        if (erro != null) return new ClienteResult(null, erro);

        if (!string.IsNullOrWhiteSpace(dto.CpfCnpj))
        {
            var digits = CnpjValidator.ApenasDigitos(dto.CpfCnpj);
            var existente = await _repo.GetByCpfCnpjAsync(request.EmpresaId, digits, ct);
            if (existente != null) return new ClienteResult(null, "Já existe cliente com este CPF/CNPJ nesta empresa.");
        }

        var cliente = Cliente.Criar(
            request.EmpresaId, (TipoPessoa)dto.TipoPessoa,
            string.IsNullOrWhiteSpace(dto.CpfCnpj) ? null : CnpjValidator.ApenasDigitos(dto.CpfCnpj),
            dto.RazaoSocial, dto.NomeFantasia, dto.Email, dto.Telefone,
            dto.Logradouro, dto.Numero, dto.Complemento, dto.Bairro, dto.Cidade,
            dto.Uf, dto.Cep, dto.CodigoMunicipio,
            string.IsNullOrWhiteSpace(dto.InscricaoEstadual) ? null : dto.InscricaoEstadual,
            (IndicadorIeDestinatario)dto.IndicadorIe);

        await _repo.AddAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);

        return new ClienteResult(MapDetalhe(cliente), null);
    }

    internal static string? ValidarCliente(
        int tipoPessoa, string? cpfCnpj, string razaoSocial,
        string logradouro, string numero, string bairro, string cidade,
        string uf, string cep, string codigoMunicipio,
        string? inscricaoEstadual, int indicadorIe)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial)) return "Razão social/Nome é obrigatório.";

        // CPF/CNPJ obrigatório exceto Estrangeiro
        var tp = (TipoPessoa)tipoPessoa;
        if (tp != TipoPessoa.Estrangeiro)
        {
            if (string.IsNullOrWhiteSpace(cpfCnpj)) return "CPF/CNPJ é obrigatório para PF/PJ.";
            var digits = CnpjValidator.ApenasDigitos(cpfCnpj);
            if (digits.Length == 14 && !CnpjValidator.Validar(cpfCnpj)) return "CNPJ inválido.";
            if (digits.Length == 11 && !CnpjValidator.ValidarCpf(cpfCnpj)) return "CPF inválido.";
            if (digits.Length is not (11 or 14)) return "CPF/CNPJ deve ter 11 ou 14 dígitos.";
        }

        if (string.IsNullOrWhiteSpace(logradouro)) return "Logradouro é obrigatório.";
        if (string.IsNullOrWhiteSpace(numero)) return "Número é obrigatório.";
        if (string.IsNullOrWhiteSpace(bairro)) return "Bairro é obrigatório.";
        if (string.IsNullOrWhiteSpace(cidade)) return "Cidade é obrigatória.";
        if (!IeValidator.UfValida(uf)) return $"UF '{uf}' inválida.";
        if (string.IsNullOrWhiteSpace(cep) || cep.Where(char.IsDigit).Count() != 8) return "CEP deve ter 8 dígitos.";
        if (string.IsNullOrWhiteSpace(codigoMunicipio) || codigoMunicipio.Where(char.IsDigit).Count() != 7)
            return "Código IBGE do município deve ter 7 dígitos.";

        // IE: se indicador = 1 (contribuinte), precisa de IE válida; se 2 ou 9, não precisa
        var indEnum = (IndicadorIeDestinatario)indicadorIe;
        if (indEnum == IndicadorIeDestinatario.Contribuinte)
        {
            if (string.IsNullOrWhiteSpace(inscricaoEstadual))
                return "Inscrição estadual é obrigatória para contribuintes do ICMS.";
            if (!IeValidator.Validar(inscricaoEstadual, uf))
                return $"Inscrição estadual inválida para a UF {uf}.";
        }

        return null;
    }

    internal static ClienteDetalheDto MapDetalhe(Cliente c) => new(
        c.Id, (int)c.TipoPessoa, c.CpfCnpj, c.RazaoSocial, c.NomeFantasia, c.Email, c.Telefone,
        c.Logradouro, c.Numero, c.Complemento, c.Bairro, c.Cidade, c.Uf, c.Cep, c.CodigoMunicipio,
        c.InscricaoEstadual, (int)c.IndicadorIe, c.Ativo);
}

public class UpdateClienteCommandHandler : IRequestHandler<UpdateClienteCommand, ClienteResult>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ClienteResult> Handle(UpdateClienteCommand request, CancellationToken ct)
    {
        var cliente = await _repo.GetByIdAsync(request.ClienteId, ct);
        if (cliente == null || cliente.EmpresaId != request.EmpresaId)
            return new ClienteResult(null, "Cliente não encontrado.");

        var dto = request.Dto;
        var erro = CreateClienteCommandHandler.ValidarCliente(
            dto.TipoPessoa, dto.CpfCnpj, dto.RazaoSocial, dto.Logradouro,
            dto.Numero, dto.Bairro, dto.Cidade, dto.Uf, dto.Cep, dto.CodigoMunicipio,
            dto.InscricaoEstadual, dto.IndicadorIe);
        if (erro != null) return new ClienteResult(null, erro);

        cliente.Atualizar(
            (TipoPessoa)dto.TipoPessoa,
            string.IsNullOrWhiteSpace(dto.CpfCnpj) ? null : CnpjValidator.ApenasDigitos(dto.CpfCnpj),
            dto.RazaoSocial, dto.NomeFantasia, dto.Email, dto.Telefone,
            dto.Logradouro, dto.Numero, dto.Complemento, dto.Bairro, dto.Cidade,
            dto.Uf, dto.Cep, dto.CodigoMunicipio,
            string.IsNullOrWhiteSpace(dto.InscricaoEstadual) ? null : dto.InscricaoEstadual,
            (IndicadorIeDestinatario)dto.IndicadorIe);

        await _repo.UpdateAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);

        return new ClienteResult(CreateClienteCommandHandler.MapDetalhe(cliente), null);
    }
}

public class ToggleAtivoClienteCommandHandler : IRequestHandler<ToggleAtivoClienteCommand, ClienteResult>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    public ToggleAtivoClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ClienteResult> Handle(ToggleAtivoClienteCommand request, CancellationToken ct)
    {
        var cliente = await _repo.GetByIdAsync(request.ClienteId, ct);
        if (cliente == null || cliente.EmpresaId != request.EmpresaId)
            return new ClienteResult(null, "Cliente não encontrado.");
        if (cliente.Ativo) cliente.Desativar(); else cliente.Ativar();
        await _repo.UpdateAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);
        return new ClienteResult(CreateClienteCommandHandler.MapDetalhe(cliente), null);
    }
}

public class DeleteClienteCommandHandler : IRequestHandler<DeleteClienteCommand, bool>
{
    private readonly IClienteRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteClienteCommandHandler(IClienteRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteClienteCommand request, CancellationToken ct)
    {
        var cliente = await _repo.GetByIdAsync(request.ClienteId, ct);
        if (cliente == null || cliente.EmpresaId != request.EmpresaId) return false;
        cliente.Delete();
        await _repo.UpdateAsync(cliente, ct);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
