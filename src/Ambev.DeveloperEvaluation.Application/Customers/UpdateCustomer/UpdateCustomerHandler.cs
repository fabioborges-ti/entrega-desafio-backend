using Ambev.DeveloperEvaluation.Application.Customers;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.UpdateCustomer;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IMapper _mapper;

    public UpdateCustomerHandler(ICustomerRepository customerRepository, IMapper mapper)
    {
        _customerRepository = customerRepository;
        _mapper = mapper;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var validator = new UpdateCustomerCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var customer = await _customerRepository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (customer == null)
            throw new KeyNotFoundException($"Cliente com ID {request.Id} não encontrado.");

        customer.Name = request.Name.Trim();

        var updated = await _customerRepository.UpdateAsync(customer, cancellationToken);
        return _mapper.Map<CustomerDto>(updated);
    }
}

