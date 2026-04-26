using Ambev.DeveloperEvaluation.Application.Carts;
using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;
using Ambev.DeveloperEvaluation.Application.Carts.GetCart;
using Ambev.DeveloperEvaluation.Application.Carts.ListCarts;
using Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Carts;

public class CartHandlersTests
{
    private static IMapper Mapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<CartDto>(Arg.Any<Cart>()).Returns(ci =>
        {
            var c = ci.Arg<Cart>();
            return new CartDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Date = c.Date.ToString("yyyy-MM-dd"),
                Products = c.LineItems.Select(li => new CartProductDto { ProductId = li.ProductId, Quantity = li.Quantity }).ToList()
            };
        });
        return mapper;
    }

    private static (ICartRepository, IInventoryRepository, IProductRepository) Repos()
        => (Substitute.For<ICartRepository>(), Substitute.For<IInventoryRepository>(), Substitute.For<IProductRepository>());

    [Fact(DisplayName = "CreateCart: comando inválido lança ValidationException")]
    public async Task CreateCart_InvalidCommand_Throws()
    {
        var (carts, inventory, products) = Repos();
        var handler = new CreateCartHandler(carts, inventory, products, Mapper());
        var act = async () => await handler.Handle(new CreateCartCommand { UserId = 0, Date = DateTime.UtcNow }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "CreateCart: agregação ultrapassa limite por produto lança ValidationException")]
    public async Task CreateCart_AggregateExceedsLimit_Throws()
    {
        var (carts, inventory, products) = Repos();
        var handler = new CreateCartHandler(carts, inventory, products, Mapper());
        var cmd = new CreateCartCommand
        {
            UserId = 1,
            Date = DateTime.UtcNow,
            Products = new List<CartLineInput>
            {
                new() { ProductId = 1, Quantity = 11 },
                new() { ProductId = 1, Quantity = 11 }
            }
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Products");
    }

    [Fact(DisplayName = "CreateCart: produto inexistente lança ValidationException")]
    public async Task CreateCart_ProductNotFound_Throws()
    {
        var (carts, inventory, products) = Repos();
        products.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var handler = new CreateCartHandler(carts, inventory, products, Mapper());
        var cmd = new CreateCartCommand
        {
            UserId = 1,
            Date = DateTime.UtcNow,
            Products = new List<CartLineInput> { new() { ProductId = 7, Quantity = 1 } }
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Products" && e.ErrorMessage.Contains("7"));
    }

    [Fact(DisplayName = "CreateCart: estoque insuficiente lança ValidationException")]
    public async Task CreateCart_InsufficientStock_Throws()
    {
        var (carts, inventory, products) = Repos();
        products.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Product { Id = 7 });
        inventory.GetByProductIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Inventory { ProductId = 7, AvailableQuantity = 1 });
        var handler = new CreateCartHandler(carts, inventory, products, Mapper());
        var cmd = new CreateCartCommand
        {
            UserId = 1,
            Date = DateTime.UtcNow,
            Products = new List<CartLineInput> { new() { ProductId = 7, Quantity = 5 } }
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Products" && e.ErrorMessage.Contains("indisponível"));
    }

    [Fact(DisplayName = "CreateCart: estoque inexistente lança ValidationException")]
    public async Task CreateCart_NoInventory_Throws()
    {
        var (carts, inventory, products) = Repos();
        products.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Product { Id = 7 });
        inventory.GetByProductIdAsync(7, Arg.Any<CancellationToken>()).Returns((Inventory?)null);
        var handler = new CreateCartHandler(carts, inventory, products, Mapper());
        var cmd = new CreateCartCommand
        {
            UserId = 1,
            Date = DateTime.UtcNow,
            Products = new List<CartLineInput> { new() { ProductId = 7, Quantity = 1 } }
        };

        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "CreateCart: válido agrega quantidades por produto e persiste")]
    public async Task CreateCart_Valid_AggregatesAndPersists()
    {
        var (carts, inventory, products) = Repos();
        products.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Product { Id = 7 });
        inventory.GetByProductIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Inventory { ProductId = 7, AvailableQuantity = 50 });
        carts.CreateWithInventoryDeductionAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>())
            .Returns(ci => { var c = ci.Arg<Cart>(); c.Id = 99; return c; });
        var handler = new CreateCartHandler(carts, inventory, products, Mapper());

        var cmd = new CreateCartCommand
        {
            UserId = 3,
            Date = new DateTime(2026, 4, 25, 10, 0, 0, DateTimeKind.Utc),
            Products = new List<CartLineInput>
            {
                new() { ProductId = 7, Quantity = 2 },
                new() { ProductId = 7, Quantity = 3 }
            }
        };

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Id.Should().Be(99);
        dto.UserId.Should().Be(3);
        dto.Products.Should().HaveCount(1);
        dto.Products[0].Quantity.Should().Be(5);
        await carts.Received(1).CreateWithInventoryDeductionAsync(Arg.Any<Cart>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetCart: id inválido lança ValidationException")]
    public async Task GetCart_Invalid_Throws()
    {
        var handler = new GetCartHandler(Substitute.For<ICartRepository>(), Mapper());
        var act = async () => await handler.Handle(new GetCartCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "GetCart: não encontrado lança KeyNotFound")]
    public async Task GetCart_NotFound_Throws()
    {
        var carts = Substitute.For<ICartRepository>();
        carts.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Cart?)null);
        var handler = new GetCartHandler(carts, Mapper());

        var act = async () => await handler.Handle(new GetCartCommand { Id = 1 }, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "GetCart: encontrado retorna DTO mapeado")]
    public async Task GetCart_Found_ReturnsDto()
    {
        var carts = Substitute.For<ICartRepository>();
        carts.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Cart { Id = 1, UserId = 2 });
        var handler = new GetCartHandler(carts, Mapper());

        var dto = await handler.Handle(new GetCartCommand { Id = 1 }, CancellationToken.None);

        dto.UserId.Should().Be(2);
    }

    [Fact(DisplayName = "DeleteCart: id inválido lança ValidationException")]
    public async Task DeleteCart_Invalid_Throws()
    {
        var handler = new DeleteCartHandler(Substitute.For<ICartRepository>(), Substitute.For<ISaleRepository>());
        var act = async () => await handler.Handle(new DeleteCartCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "DeleteCart: com venda vinculada lança ValidationException")]
    public async Task DeleteCart_LinkedToSale_Throws()
    {
        var sales = Substitute.For<ISaleRepository>();
        sales.ExistsSaleForCartAsync(5, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteCartHandler(Substitute.For<ICartRepository>(), sales);

        var act = async () => await handler.Handle(new DeleteCartCommand { Id = 5 }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Id" && e.ErrorMessage.Contains("venda vinculada"));
    }

    [Fact(DisplayName = "DeleteCart: não encontrado lança ValidationException")]
    public async Task DeleteCart_NotFound_Throws()
    {
        var sales = Substitute.For<ISaleRepository>();
        sales.ExistsSaleForCartAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        var carts = Substitute.For<ICartRepository>();
        carts.DeleteAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteCartHandler(carts, sales);

        var act = async () => await handler.Handle(new DeleteCartCommand { Id = 5 }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "Id" && e.ErrorMessage.Contains("não encontrado"));
    }

    [Fact(DisplayName = "DeleteCart: válido retorna mensagem de sucesso")]
    public async Task DeleteCart_Valid_ReturnsMessage()
    {
        var sales = Substitute.For<ISaleRepository>();
        sales.ExistsSaleForCartAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        var carts = Substitute.For<ICartRepository>();
        carts.DeleteAsync(5, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteCartHandler(carts, sales);

        var result = await handler.Handle(new DeleteCartCommand { Id = 5 }, CancellationToken.None);

        result.Message.Should().Contain("excluído");
    }

    [Fact(DisplayName = "UpdateCart: comando inválido lança ValidationException")]
    public async Task UpdateCart_Invalid_Throws()
    {
        var handler = new UpdateCartHandler(Substitute.For<ICartRepository>(), Substitute.For<IProductRepository>(), Mapper());
        var act = async () => await handler.Handle(new UpdateCartCommand { Id = 0, UserId = 0, Date = DateTime.UtcNow }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "UpdateCart: não encontrado lança KeyNotFound")]
    public async Task UpdateCart_NotFound_Throws()
    {
        var carts = Substitute.For<ICartRepository>();
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Product { Id = 1 });
        carts.GetTrackedByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Cart?)null);
        var handler = new UpdateCartHandler(carts, products, Mapper());

        var cmd = new UpdateCartCommand
        {
            Id = 99,
            UserId = 1,
            Date = DateTime.UtcNow,
            Products = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 } }
        };
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateCart: válido atualiza e mapeia DTO")]
    public async Task UpdateCart_Valid_Updates()
    {
        var carts = Substitute.For<ICartRepository>();
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Product { Id = 7 });
        var existing = new Cart { Id = 5, UserId = 1 };
        carts.GetTrackedByIdAsync(5, Arg.Any<CancellationToken>()).Returns(existing);
        carts.UpdateWithInventoryAdjustmentAsync(existing, Arg.Any<IReadOnlyList<CartLineItem>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var cart = ci.Arg<Cart>();
                cart.LineItems = ci.Arg<IReadOnlyList<CartLineItem>>().ToList();
                return cart;
            });
        var handler = new UpdateCartHandler(carts, products, Mapper());

        var cmd = new UpdateCartCommand
        {
            Id = 5,
            UserId = 22,
            Date = new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc),
            Products = new List<CartLineInput> { new() { ProductId = 7, Quantity = 4 } }
        };

        var dto = await handler.Handle(cmd, CancellationToken.None);

        existing.UserId.Should().Be(22);
        dto.UserId.Should().Be(22);
        dto.Products.Should().HaveCount(1);
        dto.Products[0].Quantity.Should().Be(4);
    }

    [Fact(DisplayName = "ListCarts: comando inválido lança ValidationException")]
    public async Task ListCarts_Invalid_Throws()
    {
        var handler = new ListCartsHandler(Substitute.For<ICartRepository>(), Mapper());
        var act = async () => await handler.Handle(new ListCartsCommand { Page = 0, Size = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "ListCarts: paginação retorna mapeamento e contagens")]
    public async Task ListCarts_Valid_Returns()
    {
        var carts = Substitute.For<ICartRepository>();
        var items = new List<Cart>
        {
            new() { Id = 1, UserId = 1, Date = DateTime.UtcNow.Date, LineItems = new List<CartLineItem>() },
            new() { Id = 2, UserId = 2, Date = DateTime.UtcNow.Date, LineItems = new List<CartLineItem>() }
        };
        carts.ListPagedAsync(1, 2, "id desc", null, Arg.Any<CancellationToken>()).Returns((items, 5));
        var handler = new ListCartsHandler(carts, Mapper());

        var result = await handler.Handle(new ListCartsCommand { Page = 1, Size = 2, Order = "id desc" }, CancellationToken.None);

        result.TotalItems.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Data.Should().HaveCount(2);
    }
}

public class CartValidatorsTests
{
    [Fact(DisplayName = "CreateCartCommandValidator: regras")]
    public void CreateCartCommandValidator_ValidationRules()
    {
        var v = new CreateCartCommandValidator();
        var ok = new CreateCartCommand
        {
            UserId = 1,
            Products = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 } }
        };
        v.Validate(ok).IsValid.Should().BeTrue();

        var noUser = new CreateCartCommand
        {
            UserId = 0,
            Products = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 } }
        };
        v.Validate(noUser).IsValid.Should().BeFalse();

        var noProductId = new CreateCartCommand
        {
            UserId = 1,
            Products = new List<CartLineInput> { new() { ProductId = 0, Quantity = 1 } }
        };
        v.Validate(noProductId).IsValid.Should().BeFalse();

        var badQty = new CreateCartCommand
        {
            UserId = 1,
            Products = new List<CartLineInput> { new() { ProductId = 1, Quantity = 0 } }
        };
        v.Validate(badQty).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "UpdateCartCommandValidator: regras")]
    public void UpdateCartCommandValidator_ValidationRules()
    {
        var v = new UpdateCartCommandValidator();
        var ok = new UpdateCartCommand
        {
            Id = 1,
            UserId = 1,
            Products = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 } }
        };
        v.Validate(ok).IsValid.Should().BeTrue();

        var noId = new UpdateCartCommand
        {
            Id = 0,
            UserId = 1,
            Products = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 } }
        };
        v.Validate(noId).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "GetCartCommandValidator: regras")]
    public void GetCartCommandValidator_ValidationRules()
    {
        var v = new GetCartCommandValidator();
        v.Validate(new GetCartCommand { Id = 1 }).IsValid.Should().BeTrue();
        v.Validate(new GetCartCommand { Id = 0 }).IsValid.Should().BeFalse();
    }

    [Fact(DisplayName = "DeleteCartCommandValidator: regras")]
    public void DeleteCartCommandValidator_ValidationRules()
    {
        var v = new DeleteCartCommandValidator();
        v.Validate(new DeleteCartCommand { Id = 1 }).IsValid.Should().BeTrue();
        v.Validate(new DeleteCartCommand { Id = 0 }).IsValid.Should().BeFalse();
    }

    [Theory(DisplayName = "ListCartsCommandValidator: regras")]
    [InlineData(1, 1, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void ListCartsCommandValidator_Validates(int page, int size, bool expected)
    {
        var v = new ListCartsCommandValidator();
        v.Validate(new ListCartsCommand { Page = page, Size = size }).IsValid.Should().Be(expected);
    }
}

public class CartAggregatedQuantityValidatorTests
{
    [Fact(DisplayName = "EnsurePerProductTotalsWithinSaleLimit: válido não lança")]
    public void EnsurePerProduct_Valid_DoesNotThrow()
    {
        var lines = new List<CartLineInput>
        {
            new() { ProductId = 1, Quantity = 2 },
            new() { ProductId = 1, Quantity = 5 },
            new() { ProductId = 2, Quantity = 10 }
        };

        var act = () => CartAggregatedQuantityValidator.EnsurePerProductTotalsWithinSaleLimit(lines);
        act.Should().NotThrow();
    }

    [Fact(DisplayName = "EnsurePerProductTotalsWithinSaleLimit: soma > 20 lança ValidationException")]
    public void EnsurePerProduct_Aggregated_OverLimit_Throws()
    {
        var lines = new List<CartLineInput>
        {
            new() { ProductId = 1, Quantity = 11 },
            new() { ProductId = 1, Quantity = 11 }
        };

        var act = () => CartAggregatedQuantityValidator.EnsurePerProductTotalsWithinSaleLimit(lines);
        act.Should().Throw<ValidationException>().Which.Errors.Should().Contain(e => e.PropertyName == "Products");
    }
}

public class CartLineInventoryValidatorTests
{
    [Fact(DisplayName = "EnsureAvailableStockAsync: estoque suficiente não lança")]
    public async Task EnsureStock_Sufficient_DoesNotThrow()
    {
        var inventory = Substitute.For<IInventoryRepository>();
        inventory.GetByProductIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Inventory { ProductId = 1, AvailableQuantity = 10 });
        var lines = new List<CartLineInput> { new() { ProductId = 1, Quantity = 5 } };

        var act = async () => await CartLineInventoryValidator.EnsureAvailableStockAsync(lines, inventory, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "EnsureAvailableStockAsync: estoque inexistente lança")]
    public async Task EnsureStock_Missing_Throws()
    {
        var inventory = Substitute.For<IInventoryRepository>();
        inventory.GetByProductIdAsync(1, Arg.Any<CancellationToken>()).Returns((Inventory?)null);
        var lines = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 } };

        var act = async () => await CartLineInventoryValidator.EnsureAvailableStockAsync(lines, inventory, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }
}

public class CartLineProductValidatorTests
{
    [Fact(DisplayName = "EnsureProductsExistAsync: produtos existentes não lança")]
    public async Task EnsureProducts_Exist_DoesNotThrow()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Product { Id = 1 });
        var lines = new List<CartLineInput> { new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 1, Quantity = 1 } };

        var act = async () => await CartLineProductValidator.EnsureProductsExistAsync(lines, products, CancellationToken.None);
        await act.Should().NotThrowAsync();
        await products.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "EnsureProductsExistAsync: produto inexistente lança ValidationException")]
    public async Task EnsureProducts_Missing_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var lines = new List<CartLineInput> { new() { ProductId = 99, Quantity = 1 } };

        var act = async () => await CartLineProductValidator.EnsureProductsExistAsync(lines, products, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }
}

