using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Customers.DeleteCustomer;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, DeleteCustomerResult>
{
    private readonly ICustomerRepository _customerRepository;

    public DeleteCustomerHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<DeleteCustomerResult> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteCustomerCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var exists = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (exists == null)
            throw new KeyNotFoundException($"Cliente com ID {request.Id} não encontrado.");

        if (await _customerRepository.HasSalesAsync(request.Id, cancellationToken))
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    string.Empty,
                    "Não é possível excluir o cliente: existem vendas vinculadas.")
            });
        }

        var deleted = await _customerRepository.DeleteAsync(request.Id, cancellationToken);
        return new DeleteCustomerResult { Deleted = deleted };
    }
}

