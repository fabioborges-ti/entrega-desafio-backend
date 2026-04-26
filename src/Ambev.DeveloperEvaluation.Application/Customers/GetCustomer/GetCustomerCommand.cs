using Ambev.DeveloperEvaluation.Application.Customers;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.GetCustomer;

public class GetCustomerCommand : IRequest<CustomerDto>
{
    public int Id { get; set; }
}

