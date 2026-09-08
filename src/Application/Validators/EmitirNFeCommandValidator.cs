using FluentValidation;
using NfeSaas.Application.Commands.EmitirNFe;
using NfeSaas.Application.DTOs;

namespace NfeSaas.Application.Validators;

/// <summary>
/// Invariantes estruturais de <see cref="EmitirNFeCommand"/> que ainda NÃO são cobertas pelas
/// validações manuais já existentes no handler (CNPJ/CFOP/NCM/CSOSN-CST/destinatário) — foco em
/// campos obrigatórios vazios e valores numéricos que não podem ser negativos.
/// </summary>
public class EmitirNFeCommandValidator : AbstractValidator<EmitirNFeCommand>
{
    public EmitirNFeCommandValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotEqual(Guid.Empty).WithMessage("EmpresaId é obrigatório.");

        RuleFor(x => x.UsuarioId)
            .NotEqual(Guid.Empty).WithMessage("UsuarioId é obrigatório.");

        RuleFor(x => x.Dados)
            .NotNull().WithMessage("Dados da nota fiscal são obrigatórios.");

        When(x => x.Dados != null, () =>
        {
            RuleFor(x => x.Dados.Itens)
                .NotEmpty().WithMessage("A nota fiscal precisa ter ao menos um item.");

            RuleForEach(x => x.Dados.Itens).SetValidator(new ItemNotaDtoValidator());

            RuleFor(x => x.Dados.Destinatario)
                .NotNull().WithMessage("Destinatário é obrigatório.");

            RuleFor(x => x.Dados.Transporte)
                .NotNull().WithMessage("Dados de transporte são obrigatórios.");

            When(x => x.Dados.Transporte != null, () =>
            {
                RuleFor(x => x.Dados.Transporte.Frete)
                    .GreaterThanOrEqualTo(0).WithMessage("Valor do frete não pode ser negativo.");
                RuleFor(x => x.Dados.Transporte.Seguro)
                    .GreaterThanOrEqualTo(0).WithMessage("Valor do seguro não pode ser negativo.");
            });

            RuleFor(x => x.Dados.Pagamento)
                .NotNull().WithMessage("Dados de pagamento são obrigatórios.");

            When(x => x.Dados.Pagamento != null, () =>
            {
                RuleFor(x => x.Dados.Pagamento.Valor)
                    .GreaterThanOrEqualTo(0).WithMessage("Valor do pagamento não pode ser negativo.");
                RuleFor(x => x.Dados.Pagamento.FormaPagamento)
                    .NotEmpty().WithMessage("Forma de pagamento é obrigatória.");
            });
        });
    }
}

public class ItemNotaDtoValidator : AbstractValidator<ItemNotaDto>
{
    public ItemNotaDtoValidator()
    {
        RuleFor(x => x.CodigoProduto)
            .NotEmpty().WithMessage("Código do produto é obrigatório.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição do item é obrigatória.");

        RuleFor(x => x.Unidade)
            .NotEmpty().WithMessage("Unidade do item é obrigatória.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade do item deve ser maior que zero.");

        RuleFor(x => x.ValorUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("Valor unitário do item não pode ser negativo.");

        RuleFor(x => x.Desconto)
            .GreaterThanOrEqualTo(0).WithMessage("Desconto do item não pode ser negativo.");

        RuleFor(x => x.Impostos)
            .NotNull().WithMessage("Impostos do item são obrigatórios.");
    }
}
