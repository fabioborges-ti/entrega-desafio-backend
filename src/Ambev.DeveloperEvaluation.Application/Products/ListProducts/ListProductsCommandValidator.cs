using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

public class ListProductsCommandValidator : AbstractValidator<ListProductsCommand>
{
    public ListProductsCommandValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
