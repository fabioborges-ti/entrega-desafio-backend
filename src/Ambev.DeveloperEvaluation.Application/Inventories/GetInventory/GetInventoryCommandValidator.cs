using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Inventories.GetInventory;

public class GetInventoryCommandValidator : AbstractValidator<GetInventoryCommand>
{
    public GetInventoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
