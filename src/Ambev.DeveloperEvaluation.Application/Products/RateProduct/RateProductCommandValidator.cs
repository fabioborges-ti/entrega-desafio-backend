using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.RateProduct;

public class RateProductCommandValidator : AbstractValidator<RateProductCommand>
{
    public RateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Rate).InclusiveBetween(1, 5);
    }
}
