using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Inventories.DeleteInventory;

public class DeleteInventoryCommandValidator : AbstractValidator<DeleteInventoryCommand>
{
    public DeleteInventoryCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
