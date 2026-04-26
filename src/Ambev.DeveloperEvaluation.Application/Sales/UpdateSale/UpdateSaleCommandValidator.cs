using Ambev.DeveloperEvaluation.Application.Sales;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

public class UpdateSaleCommandValidator : AbstractValidator<UpdateSaleCommand>
{
    public UpdateSaleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
    }
}

