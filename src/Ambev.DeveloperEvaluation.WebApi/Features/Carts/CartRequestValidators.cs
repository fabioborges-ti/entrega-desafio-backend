using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts;

public class CreateCartRequestValidator : AbstractValidator<CreateCartRequest>
{
    public CreateCartRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty().Must(BeParsableDate).WithMessage("Date deve ser uma data válida (ex.: 2020-03-02).");
        RuleFor(x => x.Products).NotNull();
        RuleForEach(x => x.Products).ChildRules(p =>
        {
            p.RuleFor(l => l.ProductId).GreaterThan(0);
            p.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("quantidade inválida");
        });
    }

    private static bool BeParsableDate(string date) =>
        DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out _);
}

public class UpdateCartRequestValidator : AbstractValidator<UpdateCartRequest>
{
    public UpdateCartRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty().Must(BeParsableDate).WithMessage("Date deve ser uma data válida (ex.: 2020-03-02).");
        RuleFor(x => x.Products).NotNull();
        RuleForEach(x => x.Products).ChildRules(p =>
        {
            p.RuleFor(l => l.ProductId).GreaterThan(0);
            p.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("quantidade inválida");
        });
    }

    private static bool BeParsableDate(string date) =>
        DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out _);
}

