using Ambev.DeveloperEvaluation.Application.Inventories;
using Ambev.DeveloperEvaluation.Application.Inventories.CreateInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.DeleteInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.GetInventory;
using Ambev.DeveloperEvaluation.Application.Inventories.ListInventories;
using Ambev.DeveloperEvaluation.Application.Inventories.UpdateInventory;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Inventories;

public class InventoryHandlersTests
{
    private static IMapper Mapper()
    {
        var mapper = Substitute.For<IMapper>();
        mapper.Map<InventoryDto>(Arg.Any<Inventory>()).Returns(ci =>
        {
            var i = ci.Arg<Inventory>();
            return new InventoryDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                AvailableQuantity = i.AvailableQuantity
            };
        });
        return mapper;
    }

    [Fact(DisplayName = "CreateInventory: comando inválido lança ValidationException")]
    public async Task CreateInventory_InvalidCommand_Throws()
    {
        var handler = new CreateInventoryHandler(Substitute.For<IInventoryRepository>(), Substitute.For<IProductRepository>(), Mapper());
        var act = async () => await handler.Handle(new CreateInventoryCommand { ProductId = 0, AvailableQuantity = -1 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "CreateInventory: produto inexistente lança ValidationException")]
    public async Task CreateInventory_ProductNotFound_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Product?)null);
        var handler = new CreateInventoryHandler(Substitute.For<IInventoryRepository>(), products, Mapper());

        var act = async () => await handler.Handle(new CreateInventoryCommand { ProductId = 5, AvailableQuantity = 10 }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "ProductId" && e.ErrorMessage.Contains("5"));
    }

    [Fact(DisplayName = "CreateInventory: já existe estoque lança ValidationException")]
    public async Task CreateInventory_AlreadyExists_Throws()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(new Product { Id = 5 });
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.ExistsForProductIdAsync(5, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateInventoryHandler(inventories, products, Mapper());

        var act = async () => await handler.Handle(new CreateInventoryCommand { ProductId = 5, AvailableQuantity = 10 }, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().Contain(e => e.PropertyName == "ProductId" && e.ErrorMessage.Contains("Já existe", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "CreateInventory: válido persiste e retorna DTO")]
    public async Task CreateInventory_Valid_Persists()
    {
        var products = Substitute.For<IProductRepository>();
        products.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(new Product { Id = 5 });
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.ExistsForProductIdAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        inventories.CreateAsync(Arg.Any<Inventory>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Inventory>());
        var handler = new CreateInventoryHandler(inventories, products, Mapper());

        var dto = await handler.Handle(new CreateInventoryCommand { ProductId = 5, AvailableQuantity = 10 }, CancellationToken.None);

        dto.ProductId.Should().Be(5);
        dto.AvailableQuantity.Should().Be(10);
    }

    [Fact(DisplayName = "GetInventory: id inválido lança ValidationException")]
    public async Task GetInventory_Invalid_Throws()
    {
        var handler = new GetInventoryHandler(Substitute.For<IInventoryRepository>(), Mapper());
        var act = async () => await handler.Handle(new GetInventoryCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "GetInventory: não encontrado lança KeyNotFound")]
    public async Task GetInventory_NotFound_Throws()
    {
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Inventory?)null);
        var handler = new GetInventoryHandler(inventories, Mapper());

        var act = async () => await handler.Handle(new GetInventoryCommand { Id = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "GetInventory: encontrado retorna DTO")]
    public async Task GetInventory_Found_ReturnsDto()
    {
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(new Inventory { Id = 1, ProductId = 7, AvailableQuantity = 99 });
        var handler = new GetInventoryHandler(inventories, Mapper());

        var dto = await handler.Handle(new GetInventoryCommand { Id = 1 }, CancellationToken.None);

        dto.Id.Should().Be(1);
        dto.AvailableQuantity.Should().Be(99);
    }

    [Fact(DisplayName = "DeleteInventory: id inválido lança ValidationException")]
    public async Task DeleteInventory_Invalid_Throws()
    {
        var handler = new DeleteInventoryHandler(Substitute.For<IInventoryRepository>());
        var act = async () => await handler.Handle(new DeleteInventoryCommand { Id = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "DeleteInventory: não encontrado lança KeyNotFound")]
    public async Task DeleteInventory_NotFound_Throws()
    {
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteInventoryHandler(inventories);

        var act = async () => await handler.Handle(new DeleteInventoryCommand { Id = 1 }, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "DeleteInventory: válido retorna mensagem")]
    public async Task DeleteInventory_Valid_ReturnsMessage()
    {
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteInventoryHandler(inventories);

        var result = await handler.Handle(new DeleteInventoryCommand { Id = 1 }, CancellationToken.None);
        result.Message.Should().Contain("excluído");
    }

    [Fact(DisplayName = "UpdateInventory: comando inválido lança ValidationException")]
    public async Task UpdateInventory_Invalid_Throws()
    {
        var handler = new UpdateInventoryHandler(Substitute.For<IInventoryRepository>(), Mapper());
        var act = async () => await handler.Handle(new UpdateInventoryCommand { Id = 0, AvailableQuantity = -1 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "UpdateInventory: não encontrado lança KeyNotFound")]
    public async Task UpdateInventory_NotFound_Throws()
    {
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.GetTrackedByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Inventory?)null);
        var handler = new UpdateInventoryHandler(inventories, Mapper());

        var act = async () => await handler.Handle(new UpdateInventoryCommand { Id = 1, AvailableQuantity = 5 }, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact(DisplayName = "UpdateInventory: válido persiste e retorna DTO")]
    public async Task UpdateInventory_Valid_Updates()
    {
        var entity = new Inventory { Id = 1, ProductId = 5, AvailableQuantity = 10 };
        var inventories = Substitute.For<IInventoryRepository>();
        inventories.GetTrackedByIdAsync(1, Arg.Any<CancellationToken>()).Returns(entity);
        inventories.UpdateAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);
        var handler = new UpdateInventoryHandler(inventories, Mapper());

        var dto = await handler.Handle(new UpdateInventoryCommand { Id = 1, AvailableQuantity = 50 }, CancellationToken.None);

        entity.AvailableQuantity.Should().Be(50);
        dto.AvailableQuantity.Should().Be(50);
    }

    [Fact(DisplayName = "ListInventories: comando inválido lança ValidationException")]
    public async Task ListInventories_Invalid_Throws()
    {
        var handler = new ListInventoriesHandler(Substitute.For<IInventoryRepository>(), Mapper());
        var act = async () => await handler.Handle(new ListInventoriesCommand { Page = 0, Size = 0 }, CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact(DisplayName = "ListInventories: paginação retorna mapeamento e contagens")]
    public async Task ListInventories_Valid_ReturnsResult()
    {
        var inventories = Substitute.For<IInventoryRepository>();
        var items = new List<Inventory>
        {
            new() { Id = 1, ProductId = 1, AvailableQuantity = 10 },
            new() { Id = 2, ProductId = 2, AvailableQuantity = 20 }
        };
        inventories.ListPagedAsync(1, 5, "title asc", Arg.Any<CancellationToken>()).Returns((items, 6));
        var handler = new ListInventoriesHandler(inventories, Mapper());

        var result = await handler.Handle(new ListInventoriesCommand { Page = 1, Size = 5, Order = "title asc" }, CancellationToken.None);

        result.TotalItems.Should().Be(6);
        result.TotalPages.Should().Be(2);
        result.Data.Should().HaveCount(2);
    }
}

public class InventoryValidatorsTests
{
    [Theory(DisplayName = "CreateInventoryCommandValidator regras")]
    [InlineData(1, 0, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, -1, false)]
    public void CreateInventoryCommandValidator_Validates(int productId, int qty, bool expected)
    {
        var v = new CreateInventoryCommandValidator();
        v.Validate(new CreateInventoryCommand { ProductId = productId, AvailableQuantity = qty }).IsValid.Should().Be(expected);
    }

    [Theory(DisplayName = "UpdateInventoryCommandValidator regras")]
    [InlineData(1, 0, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 0, false)]
    [InlineData(1, -1, false)]
    public void UpdateInventoryCommandValidator_Validates(int id, int qty, bool expected)
    {
        var v = new UpdateInventoryCommandValidator();
        v.Validate(new UpdateInventoryCommand { Id = id, AvailableQuantity = qty }).IsValid.Should().Be(expected);
    }

    [Theory(DisplayName = "GetInventoryCommandValidator regras")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-2, false)]
    public void GetInventoryCommandValidator_Validates(int id, bool expected)
    {
        var v = new GetInventoryCommandValidator();
        v.Validate(new GetInventoryCommand { Id = id }).IsValid.Should().Be(expected);
    }

    [Theory(DisplayName = "DeleteInventoryCommandValidator regras")]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void DeleteInventoryCommandValidator_Validates(int id, bool expected)
    {
        var v = new DeleteInventoryCommandValidator();
        v.Validate(new DeleteInventoryCommand { Id = id }).IsValid.Should().Be(expected);
    }

    [Theory(DisplayName = "ListInventoriesCommandValidator regras")]
    [InlineData(1, 1, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, 10, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void ListInventoriesCommandValidator_Validates(int page, int size, bool expected)
    {
        var v = new ListInventoriesCommandValidator();
        v.Validate(new ListInventoriesCommand { Page = page, Size = size }).IsValid.Should().Be(expected);
    }
}

