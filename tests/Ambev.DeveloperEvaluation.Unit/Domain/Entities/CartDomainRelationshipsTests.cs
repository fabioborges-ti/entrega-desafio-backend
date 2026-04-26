using Ambev.DeveloperEvaluation.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities;

/// <summary>
/// Relacionamentos de domínio: User 1:N Cart; Product 1:N CartLineItem.
/// </summary>
public class CartDomainRelationshipsTests
{
    #region User �?" Cart (1:N)

    [Fact(DisplayName = "User should expose an empty carts collection by default")]
    public void User_DefaultCarts_IsEmpty()
    {
        var user = new User();

        user.Carts.Should().NotBeNull();
        user.Carts.Should().BeEmpty();
    }

    [Fact(DisplayName = "User can own multiple carts (1:N)")]
    public void User_CanAttachSeveralCarts()
    {
        var user = new User { Id = 1 };
        var cartA = new Cart { Id = 10, UserId = user.Id, Date = DateTime.UtcNow };
        var cartB = new Cart { Id = 11, UserId = user.Id, Date = DateTime.UtcNow };

        user.Carts.Add(cartA);
        user.Carts.Add(cartB);

        user.Carts.Should().HaveCount(2);
        user.Carts.Should().OnlyContain(c => c.UserId == user.Id);
    }

    [Fact(DisplayName = "Cart should reference user id for FK")]
    public void Cart_UserId_MatchesOwner()
    {
        var user = new User { Id = 42 };
        var cart = new Cart { Id = 1, UserId = user.Id, Date = DateTime.UtcNow, User = user };

        cart.UserId.Should().Be(42);
        cart.User.Should().BeSameAs(user);
    }

    #endregion

    #region Product �?" CartLineItem (1:N)

    [Fact(DisplayName = "Product should expose an empty cart line collection by default")]
    public void Product_DefaultCartLineItems_IsEmpty()
    {
        var product = new Product();

        product.CartLineItems.Should().NotBeNull();
        product.CartLineItems.Should().BeEmpty();
    }

    [Fact(DisplayName = "One product can appear in many cart lines (1:N)")]
    public void Product_CanHaveManyCartLineItems()
    {
        var product = new Product { Id = 7, Title = "SKU" };
        var cart1 = new Cart { Id = 1, UserId = 1, Date = DateTime.UtcNow };
        var cart2 = new Cart { Id = 2, UserId = 1, Date = DateTime.UtcNow };
        var lineA = new CartLineItem
        {
            Id = 10,
            CartId = cart1.Id,
            ProductId = product.Id,
            Quantity = 1,
            Cart = cart1,
            Product = product
        };
        var lineB = new CartLineItem
        {
            Id = 11,
            CartId = cart2.Id,
            ProductId = product.Id,
            Quantity = 2,
            Cart = cart2,
            Product = product
        };

        product.CartLineItems.Add(lineA);
        product.CartLineItems.Add(lineB);

        product.CartLineItems.Should().HaveCount(2);
        product.CartLineItems.Should().OnlyContain(li => li.ProductId == product.Id);
    }

    [Fact(DisplayName = "Cart line item should reference product for FK")]
    public void CartLineItem_ProductId_AndNavigation_MatchProduct()
    {
        var product = new Product { Id = 99, Title = "Beer" };
        var line = new CartLineItem
        {
            Id = 1,
            CartId = 5,
            ProductId = product.Id,
            Quantity = 3,
            Product = product
        };

        line.ProductId.Should().Be(99);
        line.Product.Should().BeSameAs(product);
    }

    #endregion
}

