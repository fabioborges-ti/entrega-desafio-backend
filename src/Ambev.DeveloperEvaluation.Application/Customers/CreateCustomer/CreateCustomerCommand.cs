using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.CreateCustomer;

public class CreateCustomerCommand : IRequest<CustomerDto>
{
    public string Name { get; set; } = string.Empty;
}
