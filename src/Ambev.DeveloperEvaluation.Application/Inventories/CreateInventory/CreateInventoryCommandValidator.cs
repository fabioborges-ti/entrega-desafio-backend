using FluentValidation;



namespace Ambev.DeveloperEvaluation.Application.Inventories.CreateInventory;



public class CreateInventoryCommandValidator : AbstractValidator<CreateInventoryCommand>

{

    public CreateInventoryCommandValidator()

    {

        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.AvailableQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockAlert).GreaterThanOrEqualTo(0);
    }

}


