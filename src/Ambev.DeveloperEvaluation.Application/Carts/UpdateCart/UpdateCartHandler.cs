using Ambev.DeveloperEvaluation.Application.Carts;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;

public class UpdateCartHandler : IRequestHandler<UpdateCartCommand, CartDto>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public UpdateCartHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IMapper mapper)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<CartDto> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        var validator = new UpdateCartCommandValidator();
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

        var cart = await _cartRepository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (cart == null)
            throw new KeyNotFoundException($"Carrinho com ID {request.Id} não encontrado.");

        var date = request.Date.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.Date, DateTimeKind.Utc)
            : request.Date.ToUniversalTime();

        cart.UserId = request.UserId;
        cart.Date = date.Date;

        var newLineItems = aggregatedProducts
            .Select(p => new CartLineItem { ProductId = p.ProductId, Quantity = p.Quantity })
            .ToList();

        var updated = await _cartRepository.UpdateWithInventoryAdjustmentAsync(
            cart,
            newLineItems,
            cancellationToken);
        return _mapper.Map<CartDto>(updated);
    }
}

