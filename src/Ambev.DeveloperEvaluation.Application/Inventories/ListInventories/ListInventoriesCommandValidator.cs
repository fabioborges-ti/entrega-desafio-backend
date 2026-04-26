using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;

public class ListInventoriesCommandValidator : AbstractValidator<ListInventoriesCommand>
{
    public ListInventoriesCommandValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
