using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Inventories.UpdateInventory;

public class UpdateInventoryCommandValidator : AbstractValidator<UpdateInventoryCommand>
{
    public UpdateInventoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AvailableQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockAlert).GreaterThanOrEqualTo(0);
    }
}
