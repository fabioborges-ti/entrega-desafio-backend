using Ambev.DeveloperEvaluation.Application.Products;
using Ambev.DeveloperEvaluation.Application.Products.RateProduct;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

public class RateProductHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IProductRatingRepository _ratingRepository = Substitute.For<IProductRatingRepository>();
    private readonly RateProductHandler _handler;

    public RateProductHandlerTests()
    {
        _handler = new RateProductHandler(_productRepository, _ratingRepository);
    }

    [Fact]
    public async Task Handle_InvalidRate_ThrowsValidationException()
    {
        var act = async () => await _handler.Handle(
            new RateProductCommand { ProductId = 1, UserId = 1, Rate = 6 },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _ratingRepository.DidNotReceive().AddAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsKeyNotFoundException()
    {
        _productRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var act = async () => await _handler.Handle(
            new RateProductCommand { ProductId = 99, UserId = 1, Rate = 4 },
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidRating_ReturnsAggregateFromRepository()
    {
        var product = new Product { Id = 5, Title = "P" };
        _productRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(product);

        _ratingRepository
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)> { [5] = (4.5m, 2) });

        var result = await _handler.Handle(
            new RateProductCommand { ProductId = 5, UserId = 7, Rate = 5 },
            CancellationToken.None);

        result.Rate.Should().Be(4.5m);
        result.Count.Should().Be(2);
        await _ratingRepository.Received(1).AddAsync(5, 7, 5m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameUserRatesTwice_AddsTwoRows_AggregateReflectsBoth()
    {
        var product = new Product { Id = 1, Title = "P" };
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        _ratingRepository
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)> { [1] = (3m, 1) });

        var first = await _handler.Handle(
            new RateProductCommand { ProductId = 1, UserId = 1, Rate = 3 },
            CancellationToken.None);
        first.Rate.Should().Be(3m);
        first.Count.Should().Be(1);

        _ratingRepository
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)> { [1] = (4m, 2) });

        var second = await _handler.Handle(
            new RateProductCommand { ProductId = 1, UserId = 1, Rate = 5 },
            CancellationToken.None);
        second.Rate.Should().Be(4m);
        second.Count.Should().Be(2);

        await _ratingRepository.Received(1).AddAsync(1, 1, 3m, Arg.Any<CancellationToken>());
        await _ratingRepository.Received(1).AddAsync(1, 1, 5m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoAggregateRows_ReturnsZeroRating()
    {
        var product = new Product { Id = 3, Title = "P" };
        _productRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(product);

        _ratingRepository
            .GetAggregatesByProductIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, (decimal AverageRate, int Count)>());

        var result = await _handler.Handle(
            new RateProductCommand { ProductId = 3, UserId = 2, Rate = 1 },
            CancellationToken.None);

        result.Rate.Should().Be(0);
        result.Count.Should().Be(0);
    }
}
