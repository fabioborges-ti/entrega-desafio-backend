using Ambev.DeveloperEvaluation.Application.Inventories;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.GetInventory;

public class GetInventoryHandler : IRequestHandler<GetInventoryCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IMapper _mapper;

    public GetInventoryHandler(IInventoryRepository inventoryRepository, IMapper mapper)
    {
        _inventoryRepository = inventoryRepository;
        _mapper = mapper;
    }

    public async Task<InventoryDto> Handle(GetInventoryCommand request, CancellationToken cancellationToken)
    {
        var validator = new GetInventoryCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var entity = await _inventoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
            throw new KeyNotFoundException($"Estoque com ID {request.Id} não encontrado.");

        return _mapper.Map<InventoryDto>(entity);
    }
}

