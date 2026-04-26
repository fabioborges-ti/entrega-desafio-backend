using Ambev.DeveloperEvaluation.Application.Sales;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleCommandValidator : AbstractValidator<CreateSaleCommand>
{
    public CreateSaleCommandValidator()
    {
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty()
            .OverridePropertyName(SaleSubmissionMessages.PropertyCustomerId)
            .WithMessage("Informe o GUID do cliente (customer.id).");
        RuleFor(x => x.BranchId).NotEmpty()
            .OverridePropertyName(SaleSubmissionMessages.PropertyBranchId)
            .WithMessage("Informe o GUID da filial (branch.id).");
        RuleFor(x => x.CartId).GreaterThan(0)
            .OverridePropertyName(SaleSubmissionMessages.PropertyCartId)
            .WithMessage("Informe o identificador numérico de um carrinho existente (cartId > 0).");
        RuleFor(x => x.SaleNumber).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.SaleNumber));
    }
}

