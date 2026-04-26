using Ambev.DeveloperEvaluation.Application.Sales;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

public class UpdateSaleRequestValidator : AbstractValidator<UpdateSaleRequest>
{
    public UpdateSaleRequestValidator()
    {
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty()
            .OverridePropertyName(SaleSubmissionMessages.PropertyCustomerId)
            .WithMessage("Informe o GUID do cliente cadastrado (customerId).");
        RuleFor(x => x.BranchId).NotEmpty()
            .OverridePropertyName(SaleSubmissionMessages.PropertyBranchId)
            .WithMessage("Informe o GUID da filial cadastrada (branchId).");
        RuleFor(x => x.CartId).GreaterThan(0)
            .WithMessage("Informe cartId com o número de um carrinho existente (inteiro maior que zero).");
    }
}

