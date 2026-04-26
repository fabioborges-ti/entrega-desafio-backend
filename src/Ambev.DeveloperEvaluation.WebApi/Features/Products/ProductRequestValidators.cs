using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Products;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Image).NotEmpty().MaximumLength(2000);
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Image).NotEmpty().MaximumLength(2000);
    }
}

public class RateProductRequestValidator : AbstractValidator<RateProductRequest>
{
    public RateProductRequestValidator()
    {
        RuleFor(x => x.Rate).InclusiveBetween(1, 5);
    }
}
