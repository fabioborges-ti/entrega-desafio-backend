using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.ListProducts;
using Ambev.DeveloperEvaluation.Application.Products.ListProductsByCategory;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

public class ListProductsByCategoryHandlerTests
{
    [Fact]
    public async Task Handle_CategoryIdInvalid_ThrowsValidationException()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var ratingRepository = Substitute.For<IProductRatingRepository>();
        var mapper = Substitute.For<IMapper>();
        var handler = new ListProductsByCategoryHandler(productRepository, ratingRepository, mapper);

        var command = new ListProductsByCategoryCommand { CategoryId = 0, Page = 1, Size = 10 };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await productRepository.DidNotReceive().ListByCategoryIdPagedAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<ProductListFilterCriteria?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCategoryId_ReturnsPagedListAndAppliesRatingAggregates()
    {
        var productRepository = Substitute.For<IProductRepository>();
        var ratingRepository = Substitute.For<IProductRatingRepository>();
        var mapper = Substitute.For<IMapper>();
        var handler = new ListProductsByCategoryHandler(productRepository, ratingRepository, mapper);

        var products = new List<Product>
        {
            new()
            {
                Id = 1,
                Title = "A",
                Price = 1m,
                Description = "d",
                CategoryId = 3,
                Category = new Category { Id = 3, Name = "Eletrônicos" },
                Image = "http://i"
            }
        };

        productRepository
            .ListByCategoryIdPagedAsync(3, 1, 10, "title", null, Arg.Any<CancellationToken>())
            .Returns((products, 25));

        var dto = new ProductDto
        {
            Id = 1,
            Title = "A",
            Price = 1m,
            Description = "d",
            CategoryId = 3,
            Category = "Eletrônicos",
            Image = "http://i",
            Rating = new ProductRatingDto()
        };
        mapper.Map<ProductDto>(products[0]).Returns(dto);

        ratingRepository
            .GetAggregatesByProductIdsAsync(
                Arg.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(1)),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)> { [1] = (4.5m, 2) });

        var command = new ListProductsByCategoryCommand
        {
            CategoryId = 3,
            Page = 1,
            Size = 10,
            Order = "title"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.TotalItems.Should().Be(25);
        result.CurrentPage.Should().Be(1);
        result.TotalPages.Should().Be(3);
        result.Data.Should().HaveCount(1);
        result.Data[0].Rating.Rate.Should().Be(4.5m);
        result.Data[0].Rating.Count.Should().Be(2);

        await productRepository.Received(1).ListByCategoryIdPagedAsync(
            3,
            1,
            10,
            "title",
            null,
            Arg.Any<CancellationToken>());
    }
}

