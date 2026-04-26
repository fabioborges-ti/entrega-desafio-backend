using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRatingRepository _productRatingRepository;
    private readonly IMapper _mapper;

    public UpdateProductHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IProductRatingRepository productRatingRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _productRatingRepository = productRatingRepository;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var validator = new UpdateProductCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var product = await _productRepository.GetTrackedByIdAsync(request.Id, cancellationToken);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {request.Id} não encontrado.");

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    nameof(UpdateProductCommand.CategoryId),
                    $"Categoria com ID {request.CategoryId} não encontrada.")
            });
        }

        product.Title = request.Title;
        product.Price = request.Price;
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        product.Image = request.Image;

        var updated = await _productRepository.UpdateAsync(product, cancellationToken);
        var dto = _mapper.Map<ProductDto>(updated);
        var aggregates = await _productRatingRepository.GetAggregatesByProductIdsAsync(
            new[] { dto.Id },
            cancellationToken);
        ProductDtoRatingHelper.ApplyAggregates(new[] { dto }, aggregates);
        return dto;
    }
}

