using Ambev.DeveloperEvaluation.Application.Carts;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

public class CreateCartHandler : IRequestHandler<CreateCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public CreateCartHandler(
        ICartRepository cartRepository,
        IInventoryRepository inventoryRepository,
        IProductRepository productRepository,
        IMapper mapper)
    {
        _cartRepository = cartRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<CartDto> Handle(CreateCartCommand request, CancellationToken cancellationToken)
    {
        var validator = new CreateCartCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var aggregatedProducts = request.Products
            .GroupBy(p => p.ProductId)
            .Select(g => new CartLineInput { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        CartAggregatedQuantityValidator.EnsurePerProductTotalsWithinSaleLimit(aggregatedProducts);

        await CartLineProductValidator.EnsureProductsExistAsync(
            aggregatedProducts,
            _productRepository,
            cancellationToken);

        await CartLineInventoryValidator.EnsureAvailableStockAsync(
            aggregatedProducts,
            _inventoryRepository,
            cancellationToken);

        var date = request.Date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)
            : request.Date.ToUniversalTime();

        var cart = new Cart
        {
            UserId = request.UserId,
            Date = date.Date,
            LineItems = aggregatedProducts
                .Select(p => new CartLineItem { ProductId = p.ProductId, Quantity = p.Quantity })
                .ToList()
        };

        var created = await _cartRepository.CreateWithInventoryDeductionAsync(cart, cancellationToken);
        return _mapper.Map<CartDto>(created);
    }
}
