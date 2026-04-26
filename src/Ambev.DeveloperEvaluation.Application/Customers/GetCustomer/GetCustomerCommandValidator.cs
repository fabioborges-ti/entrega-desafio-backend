using FluentValidation;

namespace Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;

public class GetCustomerCommandValidator : AbstractValidator<GetCustomerCommand>
{
    public GetCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
