using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.RateProduct;

public class RateProductHandler : IRequestHandler<RateProductCommand, ProductRatingDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductRatingRepository _productRatingRepository;

    public RateProductHandler(
        IProductRepository productRepository,
        IProductRatingRepository productRatingRepository)
    {
        _productRepository = productRepository;
        _productRatingRepository = productRatingRepository;
    }

    public async Task<ProductRatingDto> Handle(RateProductCommand request, CancellationToken cancellationToken)
    {
        var validator = new RateProductCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {request.ProductId} não encontrado.");

        await _productRatingRepository.AddAsync(
            request.ProductId,
            request.UserId,
            request.Rate,
            cancellationToken);

        var aggregates = await _productRatingRepository.GetAggregatesByProductIdsAsync(
            new[] { request.ProductId },
            cancellationToken);

        if (!aggregates.TryGetValue(request.ProductId, out var agg))
            return new ProductRatingDto { Rate = 0, Count = 0 };

        return new ProductRatingDto { Rate = agg.AverageRate, Count = agg.Count };
    }
}

