using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.ListProductsByCategory;

public class ListProductsByCategoryHandler : IRequestHandler<ListProductsByCategoryCommand, ListProductsResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductRatingRepository _productRatingRepository;
    private readonly IMapper _mapper;

    public ListProductsByCategoryHandler(
        IProductRepository productRepository,
        IProductRatingRepository productRatingRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _productRatingRepository = productRatingRepository;
        _mapper = mapper;
    }

    public async Task<ListProductsResult> Handle(ListProductsByCategoryCommand request, CancellationToken cancellationToken)
    {
        var validator = new ListProductsByCategoryCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var (items, total) = await _productRepository.ListByCategoryIdPagedAsync(
            request.CategoryId,
            request.Page,
            request.Size,
            request.Order,
            request.Filters,
            cancellationToken);

        var totalPages = request.Size <= 0 ? 0 : (int)Math.Ceiling(total / (double)request.Size);

        var data = items.Select(p => _mapper.Map<ProductDto>(p)).ToList();
        var ids = data.Select(d => d.Id).ToList();
        var aggregates = await _productRatingRepository.GetAggregatesByProductIdsAsync(ids, cancellationToken);
        ProductDtoRatingHelper.ApplyAggregates(data, aggregates);

        return new ListProductsResult
        {
            Data = data,
            TotalItems = total,
            CurrentPage = request.Page,
            TotalPages = totalPages
        };
    }
}
