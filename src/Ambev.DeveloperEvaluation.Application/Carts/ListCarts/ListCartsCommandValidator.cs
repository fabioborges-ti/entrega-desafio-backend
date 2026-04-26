using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsCommandValidator : AbstractValidator<ListCartsCommand>
{
    public ListCartsCommandValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
