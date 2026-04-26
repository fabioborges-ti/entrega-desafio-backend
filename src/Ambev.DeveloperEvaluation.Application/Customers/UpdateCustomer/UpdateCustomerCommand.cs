using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

public class UpdateCustomerCommand : IRequest<CustomerDto>
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

