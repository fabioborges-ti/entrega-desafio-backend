using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.DeleteInventory;

public class DeleteInventoryHandler : IRequestHandler<DeleteInventoryCommand, DeleteInventoryResult>
{
    private readonly IInventoryRepository _inventoryRepository;

    public DeleteInventoryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<DeleteInventoryResult> Handle(DeleteInventoryCommand request, CancellationToken cancellationToken)
    {
        var validator = new DeleteInventoryCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var deleted = await _inventoryRepository.DeleteAsync(request.Id, cancellationToken);
        if (!deleted)
            throw new KeyNotFoundException($"Estoque com ID {request.Id} não encontrado.");

        return new DeleteInventoryResult { Message = "Estoque excluído com sucesso." };
    }
}

