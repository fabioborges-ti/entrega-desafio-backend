using Ambev.DeveloperEvaluation.Application.Customers;
using Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Customers;

public class CustomersProfile : Profile
{
    public CustomersProfile()
    {
        CreateMap<CustomerDto, CustomerResponse>();
        CreateMap<ListCustomersResult, ListCustomersResponse>();
    }
}
