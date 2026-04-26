using Ambev.DeveloperEvaluation.Application.Customers;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.ListCustomers;

public class ListCustomersHandler : IRequestHandler<ListCustomersCommand, ListCustomersResult>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public ListCustomersHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<ListCustomersResult> Handle(ListCustomersCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListCustomersCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var (items, total) = await _customerRepository.ListPagedAsync(request.Page, request.Size, cancellationToken);
        var totalPages = request.Size <= 0 ? 0 : (int)Math.Ceiling(total / (double)request.Size);

        return new ListCustomersResult
        {
            Data = items.Select(c => _mapper.Map<CustomerDto>(c)).ToList(),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = totalPages
        };
    }
}
