using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentValidation;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

public class GetProductHandler : IRequestHandler<GetProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductRatingRepository _productRatingRepository;
    private readonly IMapper _mapper;

    public GetProductHandler(
        IProductRepository productRepository,
        IProductRatingRepository productRatingRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _productRatingRepository = productRatingRepository;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(GetProductCommand request, CancellationToken cancellationToken)
    {
        var validator = new GetProductCommandValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product == null)
            throw new KeyNotFoundException($"Produto com ID {request.Id} não encontrado.");

        var dto = _mapper.Map<ProductDto>(product);
        var aggregates = await _productRatingRepository.GetAggregatesByProductIdsAsync(
            new[] { dto.Id },
            cancellationToken);
        ProductDtoRatingHelper.ApplyAggregates(new[] { dto }, aggregates);
        return dto;
    }
}

