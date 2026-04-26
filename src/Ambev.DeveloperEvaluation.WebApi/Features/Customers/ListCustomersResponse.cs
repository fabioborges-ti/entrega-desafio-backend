namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers;

public class ListCustomersResponse
{
    public IReadOnlyList<CustomerResponse> Data { get; set; } = Array.Empty<CustomerResponse>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}
