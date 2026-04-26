using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

public class ListCustomersCommandValidator : AbstractValidator<ListCustomersCommand>
{
    public ListCustomersCommandValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Size).InclusiveBetween(1, 100);
    }
}
