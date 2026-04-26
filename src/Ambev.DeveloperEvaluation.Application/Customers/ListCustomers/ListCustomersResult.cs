using Ambev.DeveloperEvaluation.Application.Customers;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

public class ListCustomersResult
{
    public IReadOnlyList<CustomerDto> Data { get; set; } = Array.Empty<CustomerDto>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}
