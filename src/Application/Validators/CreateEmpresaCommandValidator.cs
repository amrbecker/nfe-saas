using FluentValidation;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Validators;

public class CreateEmpresaCommandValidator : AbstractValidator<CreateEmpresaCommand>
{
    public CreateEmpresaCommandValidator()
    {
        RuleFor(x => x.EscritorioId)
            .NotEqual(Guid.Empty).WithMessage("EscritorioId é obrigatório.");

        RuleFor(x => x.Dto).NotNull().WithMessage("Dados da empresa são obrigatórios.");

        When(x => x.Dto != null, () =>
        {
            RuleFor(x => x.Dto.RazaoSocial)
                .NotEmpty().WithMessage("Razão social é obrigatória.")
                .MaximumLength(200).WithMessage("Razão social deve ter no máximo 200 caracteres.");

            RuleFor(x => x.Dto.NomeFantasia)
                .NotEmpty().WithMessage("Nome fantasia é obrigatório.")
                .MaximumLength(200).WithMessage("Nome fantasia deve ter no máximo 200 caracteres.");

            RuleFor(x => x.Dto.Cnpj)
                .NotEmpty().WithMessage("CNPJ é obrigatório.")
                .Must(cnpj => CnpjValidator.Validar(cnpj)).WithMessage("CNPJ inválido.");

            RuleFor(x => x.Dto.InscricaoEstadual)
                .NotEmpty().WithMessage("Inscrição estadual é obrigatória.");

            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail em formato inválido.")
                .MaximumLength(200).WithMessage("E-mail deve ter no máximo 200 caracteres.");

            RuleFor(x => x.Dto.Telefone)
                .NotEmpty().WithMessage("Telefone é obrigatório.")
                .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.");

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
                .Length(2).WithMessage("UF deve ter 2 letras.")
                .Must(uf => IeValidator.UfValida(uf)).WithMessage("UF inválida.");

            RuleFor(x => x.Dto.Cep)
                .NotEmpty().WithMessage("CEP é obrigatório.")
                .Must(cep => cep != null && cep.Count(char.IsDigit) == 8)
                .WithMessage("CEP deve ter 8 dígitos.");

            RuleFor(x => x.Dto.CodigoMunicipio)
                .NotEmpty().WithMessage("Código IBGE do município é obrigatório.")
                .Must(cod => cod != null && cod.Count(char.IsDigit) == 7)
                .WithMessage("Código IBGE do município deve ter 7 dígitos.");

            RuleFor(x => x.Dto.RegimeTributario)
                .Must(r => r is 1 or 2 or 3)
                .WithMessage("Regime tributário inválido.");

            RuleFor(x => x.Dto.AmbienteSefaz)
                .Must(a => a is 1 or 2)
                .WithMessage("Ambiente SEFAZ inválido.");

            RuleFor(x => x.Dto.Cnae)
                .Must(cnae => CnaeValidator.Validar(cnae))
                .WithMessage("CNAE inválido — deve ter 7 dígitos.")
                .When(x => !string.IsNullOrWhiteSpace(x.Dto.Cnae));
        });
    }
}
