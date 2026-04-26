using Ambev.DeveloperEvaluation.Application.Inventories;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;

public class ListInventoriesHandler : IRequestHandler<ListInventoriesCommand, ListInventoriesResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IMapper _mapper;

    public ListInventoriesHandler(IInventoryRepository inventoryRepository, IMapper mapper)
    {
        _inventoryRepository = inventoryRepository;
        _mapper = mapper;
    }

    public async Task<ListInventoriesResult> Handle(ListInventoriesCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListInventoriesCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var (items, total) = await _inventoryRepository.ListPagedAsync(
            request.Page,
            request.Size,
            request.Order,
            cancellationToken);

        var totalPages = request.Size <= 0 ? 0 : (int)Math.Ceiling(total / (double)request.Size);

        return new ListInventoriesResult
        {
            Data = items.Select(i => _mapper.Map<InventoryDto>(i)).ToList(),
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = totalPages
        };
    }
}
