using Ambev.DeveloperEvaluation.Application.Inventories;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.UpdateInventory;

public class UpdateInventoryHandler : IRequestHandler<UpdateInventoryCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IMapper _mapper;

    public UpdateInventoryHandler(IInventoryRepository inventoryRepository, IMapper mapper)
    {
        _inventoryRepository = inventoryRepository;
        _mapper = mapper;
    }

    public async Task<InventoryDto> Handle(UpdateInventoryCommand request, CancellationToken cancellationToken)
    {
        var validator = new UpdateInventoryCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var entity = await _inventoryRepository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"Estoque com ID {request.Id} não encontrado.");

        entity.AvailableQuantity = request.AvailableQuantity;
        entity.MinimumStockAlert = request.MinimumStockAlert;

        var updated = await _inventoryRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<InventoryDto>(updated);
    }
}

