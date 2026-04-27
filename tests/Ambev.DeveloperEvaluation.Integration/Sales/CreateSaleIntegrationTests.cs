using Ambev.DeveloperEvaluation.Application.Sales;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.Events;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Infrastructure;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateSaleIntegrationTests
{
    private readonly PostgresIntegrationTestFixture _fixture;

    public CreateSaleIntegrationTests(PostgresIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Handle_WithValidPersistedCart_CreatesSaleAndItemsInPostgres()
    {
        await _fixture.ResetDatabaseAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedSaleScenarioAsync(db);

        var handler = CreateHandler(db);

        var result = await handler.Handle(new CreateSaleCommand
        {
            SaleDate = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
            SaleNumber = "IT-SALE-0001",
            CustomerId = seed.CustomerId,
            BranchId = seed.BranchId,
            CartId = seed.CartId,
            SuppressEventPublication = true
        }, CancellationToken.None);

        await using var assertionDb = _fixture.CreateContext();
        var persistedSale = await assertionDb.Sales
            .Include(s => s.Items)
            .SingleAsync(s => s.Id == result.Id);

        Assert.Equal("IT-SALE-0001", result.SaleNumber);
        Assert.Equal(seed.CartId, result.CartId);
        Assert.Equal(45m, result.TotalAmount);

        Assert.Equal(seed.CustomerId, persistedSale.CustomerId);
        Assert.Equal(seed.BranchId, persistedSale.BranchId);
        Assert.Equal(seed.CartId, persistedSale.CartId);
        Assert.False(persistedSale.IsCancelled);
        Assert.Equal(45m, persistedSale.TotalAmount);

        var item = Assert.Single(persistedSale.Items);
        Assert.Equal(seed.ProductId, item.ProductId);
        Assert.Equal(seed.Quantity, item.Quantity);
        Assert.Equal(10m, item.UnitPrice);
        Assert.Equal(0.10m, item.DiscountPercent);
        Assert.Equal(5m, item.DiscountAmount);
        Assert.Equal(45m, item.LineTotal);

        var inventory = await assertionDb.Inventories.SingleAsync(i => i.ProductId == seed.ProductId);
        Assert.Equal(seed.InitialStock - seed.Quantity, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task Handle_WithUnknownCustomer_DoesNotCreateSaleAndThrowsValidationException()
    {
        await _fixture.ResetDatabaseAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedSaleScenarioAsync(db);
        var handler = CreateHandler(db);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new CreateSaleCommand
            {
                SaleDate = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
                SaleNumber = "IT-SALE-INVALID-CUSTOMER",
                CustomerId = 999_999,
                BranchId = seed.BranchId,
                CartId = seed.CartId,
                SuppressEventPublication = true
            }, CancellationToken.None));

        Assert.Contains(exception.Errors, error =>
            error.PropertyName == SaleSubmissionMessages.PropertyCustomerId);

        await using var assertionDb = _fixture.CreateContext();
        Assert.Empty(await assertionDb.Sales.ToListAsync());
    }

    private static CreateSaleHandler CreateHandler(DefaultContext db) =>
        new(
            new SaleRepository(db),
            new ProductRepository(db),
            new CustomerRepository(db),
            new BranchRepository(db),
            new CartRepository(db),
            new NoOpSaleEventPublisher(),
            NullLogger<CreateSaleHandler>.Instance);

    private static async Task<SaleScenarioSeed> SeedSaleScenarioAsync(DefaultContext db)
    {
        var user = new User
        {
            Username = "integration.admin",
            Email = "integration.admin@example.test",
            Phone = "+5511999999999",
            Password = "hashed-password",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            Name = new UserPersonName
            {
                FirstName = "Integration",
                LastName = "Admin"
            },
            Address = new UserAddress
            {
                City = "Sao Paulo",
                Street = "Rua Teste",
                Number = 123,
                Zipcode = "01000-000",
                Geolocation = new AddressGeolocation
                {
                    Lat = "0",
                    Long = "0"
                }
            }
        };

        var customer = new Customer { Name = "Cliente Integracao" };
        var category = new Category { Name = "Categoria Integracao" };

        db.Users.Add(user);
        db.Customers.Add(customer);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var branch = new Branch
        {
            Name = "Filial Integracao",
            Cnpj = "91000000000001",
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        const int initialStock = 20;
        var product = new Product
        {
            Title = "Produto Integracao",
            Description = "Produto usado no teste de integracao de venda.",
            Price = 10m,
            CategoryId = category.Id,
            Image = "https://example.test/product.png",
            Inventory = new Inventory
            {
                AvailableQuantity = initialStock,
                MinimumStockAlert = 0
            }
        };

        db.Branches.Add(branch);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        const int quantity = 5;
        var cart = await new CartRepository(db).CreateWithInventoryDeductionAsync(new Cart
        {
            UserId = user.Id,
            Date = DateTime.UtcNow,
            LineItems =
            {
                new CartLineItem
                {
                    ProductId = product.Id,
                    Quantity = quantity
                }
            }
        });

        return new SaleScenarioSeed(
            customer.Id,
            branch.Id,
            product.Id,
            cart.Id,
            initialStock,
            quantity);
    }

    private sealed record SaleScenarioSeed(
        int CustomerId,
        int BranchId,
        int ProductId,
        int CartId,
        int InitialStock,
        int Quantity);

    private sealed class NoOpSaleEventPublisher : ISaleEventPublisher
    {
        public void PublishSaleCreated(SaleCreatedPayload payload)
        {
        }

        public void PublishSaleModified(SaleModifiedPayload payload)
        {
        }

        public void PublishSaleCancelled(SaleCancelledPayload payload)
        {
        }
    }
}
