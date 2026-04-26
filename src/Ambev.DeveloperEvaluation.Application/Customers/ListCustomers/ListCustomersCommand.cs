using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

public class ListCustomersCommand : IRequest<ListCustomersResult>
{
    public int Page { get; set; } = 1;

    public int Size { get; set; } = 10;
}
