using FluentValidation;



namespace Ambev.DeveloperEvaluation.WebApi.Features.Inventories;



public class CreateInventoryRequestValidator : AbstractValidator<CreateInventoryRequest>

{

    public CreateInventoryRequestValidator()

    {

        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.AvailableQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockAlert).GreaterThanOrEqualTo(0);
    }

}



public class UpdateInventoryRequestValidator : AbstractValidator<UpdateInventoryRequest>

{

    public UpdateInventoryRequestValidator()

    {

        RuleFor(x => x.AvailableQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockAlert).GreaterThanOrEqualTo(0);
    }
}


