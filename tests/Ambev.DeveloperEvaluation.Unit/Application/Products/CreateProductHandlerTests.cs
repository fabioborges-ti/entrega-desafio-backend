using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.CreateProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

public class CreateProductHandlerTests
{
    [Fact]
    public async Task Handle_CreateAsyncReceivesProductWithoutUserRatings_CommandHasNoRatingFields()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var ratingRepository = Substitute.For<IProductRatingRepository>();
        var mapper = Substitute.For<IMapper>();

        var handler = new CreateProductHandler(productRepository, categoryRepository, ratingRepository, mapper);

        var category = new Category { Id = 2, Name = "Cat" };
        categoryRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(category);

        Product? captured = null;
        productRepository
            .CreateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<Product>();
                captured.Id = 100;
                return captured;
            });

        var dto = new ProductDto { Id = 100, Title = "T", Category = "Cat" };
        mapper.Map<ProductDto>(Arg.Any<Product>()).Returns(dto);

        ratingRepository
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)>());

        var command = new CreateProductCommand
        {
            Title = "T",
            Price = 10m,
            Description = "D",
            CategoryId = 2,
            Image = "http://img"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(100);
        captured.Should().NotBeNull();
        captured!.UserRatings.Should().BeEmpty();
        typeof(CreateProductCommand).GetProperty("RatingRate").Should().BeNull();
        typeof(CreateProductCommand).GetProperty("RatingCount").Should().BeNull();
        result.Rating.Rate.Should().Be(0);
        result.Rating.Count.Should().Be(0);
    }
}
