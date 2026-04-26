using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;

public class DeleteCustomerCommand : IRequest<DeleteCustomerResult>
{
    public int Id { get; set; }
}

