using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

public class SaleItemTests
{
    [Theory(DisplayName = "RecalculatePricing aplica políticas de desconto e calcula totais")]
    [InlineData(1, 100, 0, 100)]
    [InlineData(3, 100, 0, 300)]
    [InlineData(4, 100, 0.10, 360)]
    [InlineData(9, 100, 0.10, 810)]
    [InlineData(10, 100, 0.20, 800)]
    [InlineData(20, 100, 0.20, 1600)]
    public void RecalculatePricing_AppliesPolicyAndComputesTotals(
        int quantity,
        decimal unitPrice,
        decimal expectedDiscountPercent,
        decimal expectedLineTotal)
    {
        var item = new SaleItem
        {
            ProductId = 1,
            Quantity = quantity,
            UnitPrice = unitPrice
        };

        item.RecalculatePricing();

        item.DiscountPercent.Should().Be(expectedDiscountPercent);
        item.LineTotal.Should().Be(expectedLineTotal);
        item.DiscountAmount.Should().Be(quantity * unitPrice - expectedLineTotal);
    }

    [Fact(DisplayName = "RecalculatePricing arredonda usando MidpointRounding.AwayFromZero")]
    public void RecalculatePricing_RoundsHalfAway()
    {
        var item = new SaleItem
        {
            ProductId = 1,
            Quantity = 4,
            UnitPrice = 9.999m
        };

        item.RecalculatePricing();

        item.DiscountAmount.Should().Be(Math.Round(4 * 9.999m * 0.10m, 2, MidpointRounding.AwayFromZero));
        item.LineTotal.Should().Be(Math.Round(4 * 9.999m - item.DiscountAmount, 2, MidpointRounding.AwayFromZero));
    }

    [Theory(DisplayName = "RecalculatePricing falha quando quantidade é inválida")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void RecalculatePricing_WithInvalidQuantity_Throws(int quantity)
    {
        var item = new SaleItem
        {
            ProductId = 1,
            Quantity = quantity,
            UnitPrice = 10m
        };

        var act = () => item.RecalculatePricing();

        act.Should().Throw<DomainException>();
    }
}

