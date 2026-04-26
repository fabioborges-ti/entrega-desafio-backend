using Ambev.DeveloperEvaluation.Application.Inventories;

using Ambev.DeveloperEvaluation.Domain.Entities;

using Ambev.DeveloperEvaluation.Domain.Repositories;

using AutoMapper;

using FluentValidation;

using MediatR;



namespace Ambev.DeveloperEvaluation.Application.Inventories.CreateInventory;



public class CreateInventoryHandler : IRequestHandler<CreateInventoryCommand, InventoryDto>

{

    private readonly IInventoryRepository _inventoryRepository;

    private readonly IProductRepository _productRepository;

    private readonly IMapper _mapper;



    public CreateInventoryHandler(

        IInventoryRepository inventoryRepository,

        IProductRepository productRepository,

        IMapper mapper)

    {

        _inventoryRepository = inventoryRepository;

        _productRepository = productRepository;

        _mapper = mapper;

    }



    public async Task<InventoryDto> Handle(CreateInventoryCommand request, CancellationToken cancellationToken)

    {

        var validator = new CreateInventoryCommandValidator();

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)

            throw new ValidationException(validationResult.Errors);



        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)

        {

            throw new ValidationException(new[]

            {

                new FluentValidation.Results.ValidationFailure(

                    nameof(CreateInventoryCommand.ProductId),

                    $"Produto com ID {request.ProductId} não encontrado.")

            });

        }



        if (await _inventoryRepository.ExistsForProductIdAsync(request.ProductId, cancellationToken))

        {

            throw new ValidationException(new[]

            {

                new FluentValidation.Results.ValidationFailure(

                    nameof(CreateInventoryCommand.ProductId),

                    "Já existe estoque cadastrado para este produto.")

            });

        }



        var inventory = new Inventory
        {
            ProductId = request.ProductId,
            AvailableQuantity = request.AvailableQuantity,
            MinimumStockAlert = request.MinimumStockAlert
        };



        var created = await _inventoryRepository.CreateAsync(inventory, cancellationToken);

        return _mapper.Map<InventoryDto>(created);

    }

}



