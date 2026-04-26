using Ambev.DeveloperEvaluation.Application.Products;

using Ambev.DeveloperEvaluation.Domain.Entities;

using Ambev.DeveloperEvaluation.Domain.Repositories;

using AutoMapper;

using FluentValidation;

using MediatR;



namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;



public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>

{

    private readonly IProductRepository _productRepository;

    private readonly ICategoryRepository _categoryRepository;

    private readonly IProductRatingRepository _productRatingRepository;

    private readonly IMapper _mapper;



    public CreateProductHandler(

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



    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)

    {

        var validator = new CreateProductCommandValidator();

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)

            throw new ValidationException(validationResult.Errors);



        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category == null)

        {

            throw new ValidationException(new[]

            {

                new FluentValidation.Results.ValidationFailure(

                    nameof(CreateProductCommand.CategoryId),

                    $"Categoria com ID {request.CategoryId} não encontrada.")

            });

        }



        var product = new Product

        {

            Title = request.Title,

            Price = request.Price,

            Description = request.Description,

            CategoryId = request.CategoryId,

            Image = request.Image,

            Inventory = new Inventory { AvailableQuantity = 0 }

        };



        var created = await _productRepository.CreateAsync(product, cancellationToken);

        var dto = _mapper.Map<ProductDto>(created);
        var aggregates = await _productRatingRepository.GetAggregatesByProductIdsAsync(
            new[] { dto.Id },
            cancellationToken);
        ProductDtoRatingHelper.ApplyAggregates(new[] { dto }, aggregates);
        return dto;

    }

}



