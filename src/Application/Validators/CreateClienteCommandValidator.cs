using FluentValidation;
using NfeSaas.Application.Commands.ClienteCommands;
using NfeSaas.Domain.Entities;
using NfeSaas.Domain.Enums;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Validators;

/// <summary>
/// O handler já valida (via <c>CreateClienteCommandHandler.ValidarCliente</c>) campos obrigatórios,
/// CPF/CNPJ e IE — aqui reforçamos essas mesmas invariantes na camada de pipeline (chegam antes do
/// handler) e adicionamos o que ainda faltava: formato de e-mail quando informado.
/// </summary>
public class CreateClienteCommandValidator : AbstractValidator<CreateClienteCommand>
{
    public CreateClienteCommandValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotEqual(Guid.Empty).WithMessage("EmpresaId é obrigatório.");

        RuleFor(x => x.Dto).NotNull().WithMessage("Dados do cliente são obrigatórios.");

        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.RazaoSocial)
                .NotEmpty().WithMessage("Razão social/Nome é obrigatório.");

            RuleFor(x => x.Dto.CpfCnpj)
                .NotEmpty().WithMessage("CPF/CNPJ é obrigatório para PF/PJ.")
                .When(x => (TipoPessoa)x.Dto.TipoPessoa != TipoPessoa.Estrangeiro);

            RuleFor(x => x.Dto.CpfCnpj)
                .Must(cpfCnpj =>
                {
                    var digitos = CnpjValidator.ApenasDigitos(cpfCnpj);
                    return digitos.Length switch
                    {
                        14 => CnpjValidator.Validar(cpfCnpj),
                        11 => CnpjValidator.ValidarCpf(cpfCnpj),
                        _ => false
                    };
                })
                .WithMessage("CPF/CNPJ inválido.")
                .When(x => (TipoPessoa)x.Dto.TipoPessoa != TipoPessoa.Estrangeiro
                    && !string.IsNullOrWhiteSpace(x.Dto.CpfCnpj));

            RuleFor(x => x.Dto.Email)
                .EmailAddress().WithMessage("E-mail em formato inválido.")
                .When(x => !string.IsNullOrWhiteSpace(x.Dto.Email));

            RuleFor(x => x.Dto.Logradouro)
                .NotEmpty().WithMessage("Logradouro é obrigatório.");

            RuleFor(x => x.Dto.Numero)
                .NotEmpty().WithMessage("Número é obrigatório.");

            RuleFor(x => x.Dto.Bairro)
                .NotEmpty().WithMessage("Bairro é obrigatório.");

            RuleFor(x => x.Dto.Cidade)
                .NotEmpty().WithMessage("Cidade é obrigatória.");

            RuleFor(x => x.Dto.Uf)
                .NotEmpty().WithMessage("UF é obrigatória.")
                .Must(uf => IeValidator.UfValida(uf)).WithMessage("UF inválida.");

            RuleFor(x => x.Dto.Cep)
                .NotEmpty().WithMessage("CEP é obrigatório.")
                .Must(cep => cep != null && cep.Count(char.IsDigit) == 8)
                .WithMessage("CEP deve ter 8 dígitos.");

            RuleFor(x => x.Dto.CodigoMunicipio)
                .NotEmpty().WithMessage("Código IBGE do município é obrigatório.")
                .Must(cod => cod != null && cod.Count(char.IsDigit) == 7)
                .WithMessage("Código IBGE do município deve ter 7 dígitos.");

            RuleFor(x => x.Dto.InscricaoEstadual)
                .NotEmpty().WithMessage("Inscrição estadual é obrigatória para contribuintes do ICMS.")
                .Must((cmd, ie) => IeValidator.Validar(ie, cmd.Dto.Uf))
                .WithMessage("Inscrição estadual inválida para a UF informada.")
                .When(x => (IndicadorIeDestinatario)x.Dto.IndicadorIe == IndicadorIeDestinatario.Contribuinte);
        });
    }
}
