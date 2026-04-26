using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;

public class UpdateCartCommandValidator : AbstractValidator<UpdateCartCommand>
{
    public UpdateCartCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Products).NotNull();
        RuleForEach(x => x.Products).ChildRules(p =>
        {
            p.RuleFor(l => l.ProductId).GreaterThan(0);
            p.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("quantidade inválida");
        });
    }
}

