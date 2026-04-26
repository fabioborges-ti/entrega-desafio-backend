using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

public class GetProductCommandValidator : AbstractValidator<GetProductCommand>
{
    public GetProductCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
