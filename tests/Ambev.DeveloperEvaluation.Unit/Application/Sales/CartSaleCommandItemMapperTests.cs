using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CartSaleCommandItemMapperTests
{
    [Fact]
    public void FromCart_AggregatesSameProductIdIntoSingleLineWithSummedQuantity()
    {
        var cart = new Cart
        {
            Id = 1,
            LineItems = new List<CartLineItem>
            {
                new() { Id = 2, CartId = 1, ProductId = 5, Quantity = 10 },
                new() { Id = 3, CartId = 1, ProductId = 5, Quantity = 10 }
            }
        };

        var result = CartSaleCommandItemMapper.FromCart(cart);

        result.Should().ContainSingle();
        result[0].ProductId.Should().Be(5);
        result[0].Quantity.Should().Be(20);
    }

    [Fact]
    public void FromCart_OrdersDtosByProductId()
    {
        var cart = new Cart
        {
            Id = 1,
            LineItems = new List<CartLineItem>
            {
                new() { Id = 1, CartId = 1, ProductId = 30, Quantity = 1 },
                new() { Id = 2, CartId = 1, ProductId = 10, Quantity = 2 }
            }
        };

        var result = CartSaleCommandItemMapper.FromCart(cart);

        result.Should().HaveCount(2);
        result[0].ProductId.Should().Be(10);
        result[1].ProductId.Should().Be(30);
    }

    [Fact]
    public void FromCart_NullLineItems_TreatedAsEmpty()
    {
        var cart = new Cart { Id = 1, LineItems = null! };

        var result = CartSaleCommandItemMapper.FromCart(cart);

        result.Should().BeEmpty();
    }

    [Fact]
    public void RecalculatePricing_OnAggregatedQuantity_AppliesTierDiscount()
    {
        var item = new SaleItem
        {
            ProductId = 1,
            Quantity = 20,
            UnitPrice = 100m
        };

        item.RecalculatePricing();

        item.DiscountPercent.Should().Be(0.20m);
        item.LineTotal.Should().Be(1600m);
    }
}
