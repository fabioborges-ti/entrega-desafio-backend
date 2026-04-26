using Ambev.DeveloperEvaluation.Application.Products;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Products;

public class ProductDtoRatingHelperTests
{
    [Fact]
    public void ApplyAggregates_WhenNoRows_SetsZeroRating()
    {
        var dtos = new List<ProductDto>
        {
            new() { Id = 1, Title = "A", Rating = new ProductRatingDto { Rate = 99, Count = 99 } }
        };

        ProductDtoRatingHelper.ApplyAggregates(dtos, new Dictionary<int, (decimal, int)>());

        dtos[0].Rating.Rate.Should().Be(0);
        dtos[0].Rating.Count.Should().Be(0);
    }

    [Fact]
    public void ApplyAggregates_WhenAggregatesExist_SetsRateAndCount()
    {
        var dtos = new List<ProductDto>
        {
            new() { Id = 10, Title = "X" },
            new() { Id = 20, Title = "Y" }
        };

        var agg = new Dictionary<int, (decimal AverageRate, int Count)>
        {
            [10] = (3.33m, 3),
            [20] = (5m, 1)
        };

        ProductDtoRatingHelper.ApplyAggregates(dtos, agg);

        dtos[0].Rating.Rate.Should().Be(3.33m);
        dtos[0].Rating.Count.Should().Be(3);
        dtos[1].Rating.Rate.Should().Be(5m);
        dtos[1].Rating.Count.Should().Be(1);
    }
}
