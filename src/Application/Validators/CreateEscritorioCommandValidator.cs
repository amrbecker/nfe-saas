using FluentValidation;
using NfeSaas.Application.Commands.EscritorioCommands;
using NfeSaas.Domain.Services;

namespace NfeSaas.Application.Validators;

/// <summary>
/// Único endpoint de escrita público (auto-cadastro, sem autenticação) do sistema — validação de
/// entrada é a primeira linha de defesa aqui: campos obrigatórios, formato de e-mail, tamanhos
/// razoáveis (evitar payloads absurdos) e CNPJ com dígito verificador válido.
/// </summary>
public class CreateEscritorioCommandValidator : AbstractValidator<CreateEscritorioCommand>
{
    public CreateEscritorioCommandValidator()
    {
        RuleFor(x => x.Dto).NotNull().WithMessage("Dados do escritório são obrigatórios.");

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

            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório.")
                .EmailAddress().WithMessage("E-mail em formato inválido.")
                .MaximumLength(200).WithMessage("E-mail deve ter no máximo 200 caracteres.");

            RuleFor(x => x.Dto.Telefone)
                .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Dto.Telefone));

            RuleFor(x => x.Dto.Plano)
                .Must(p => p is 1 or 2 or 3)
                .WithMessage("Plano inválido — escolha Básico, Profissional ou Enterprise.");

            RuleFor(x => x.Dto.NomeAdmin)
                .NotEmpty().WithMessage("Nome do administrador é obrigatório.")
                .MaximumLength(200).WithMessage("Nome do administrador deve ter no máximo 200 caracteres.");

            RuleFor(x => x.Dto.EmailAdmin)
                .NotEmpty().WithMessage("E-mail do administrador é obrigatório.")
                .EmailAddress().WithMessage("E-mail do administrador em formato inválido.")
                .MaximumLength(200).WithMessage("E-mail do administrador deve ter no máximo 200 caracteres.");

            RuleFor(x => x.Dto.SenhaAdmin)
                .NotEmpty().WithMessage("Senha do administrador é obrigatória.")
                .MinimumLength(8).WithMessage("Senha do administrador deve ter ao menos 8 caracteres.")
                .MaximumLength(200).WithMessage("Senha do administrador deve ter no máximo 200 caracteres.");
        });
    }
}
