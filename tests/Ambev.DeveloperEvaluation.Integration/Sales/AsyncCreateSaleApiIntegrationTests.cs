using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Enums;
using Ambev.DeveloperEvaluation.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Integration.Infrastructure;
using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.ORM.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

[Collection(AsyncSalesApiTestCollection.Name)]
public sealed class AsyncCreateSaleApiIntegrationTests
{
    private readonly AsyncSalesApiTestFixture _fixture;

    public AsyncCreateSaleApiIntegrationTests(AsyncSalesApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostSales_WithValidCart_ReturnsAcceptedAndConsumerPersistsSale()
    {
        await _fixture.ResetDatabaseAsync();

        await using var db = _fixture.CreateContext();
        var seed = await SeedSaleScenarioAsync(db);

        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwtFor(seed.User));

        var response = await client.PostAsJsonAsync("/api/sales", new
        {
            saleDate = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc),
            saleNumber = "IT-ASYNC-SALE-0001",
            customerId = seed.CustomerId,
            branchId = seed.BranchId,
            cartId = seed.CartId
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(
            body.RootElement.GetProperty("data").GetProperty("correlationId").GetString()));

        var sale = await WaitForSaleAsync("IT-ASYNC-SALE-0001", TimeSpan.FromSeconds(20));

        Assert.Equal(seed.CustomerId, sale.CustomerId);
        Assert.Equal(seed.BranchId, sale.BranchId);
        Assert.Equal(seed.CartId, sale.CartId);
        Assert.Equal(81m, sale.TotalAmount);

        var item = Assert.Single(sale.Items);
        Assert.Equal(seed.ProductId, item.ProductId);
        Assert.Equal(seed.Quantity, item.Quantity);
        Assert.Equal(15m, item.UnitPrice);
        Assert.Equal(0.10m, item.DiscountPercent);
        Assert.Equal(9m, item.DiscountAmount);
        Assert.Equal(81m, item.LineTotal);
    }

    private async Task<Sale> WaitForSaleAsync(string saleNumber, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            await using var db = _fixture.CreateContext();
            var sale = await db.Sales
                .Include(s => s.Items)
                .SingleOrDefaultAsync(s => s.SaleNumber == saleNumber, cts.Token);

            if (sale != null)
                return sale;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cts.Token);
        }

        throw new TimeoutException($"A venda '{saleNumber}' não foi persistida pelo consumer dentro do tempo esperado.");
    }

    private static string CreateJwtFor(User user)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = AsyncSalesApiTestFixture.JwtSecretKey
            })
            .Build();

        return new JwtTokenGenerator(configuration).GenerateToken(user);
    }

    private static async Task<SaleScenarioSeed> SeedSaleScenarioAsync(DefaultContext db)
    {
        var user = new User
        {
            Username = "integration.customer",
            Email = "integration.customer@example.test",
            Phone = "+5511988888888",
            Password = "hashed-password",
            Role = UserRole.Customer,
            Status = UserStatus.Active,
            Name = new UserPersonName
            {
                FirstName = "Integration",
                LastName = "Customer"
            },
            Address = new UserAddress
            {
                City = "Sao Paulo",
                Street = "Rua Teste",
                Number = 456,
                Zipcode = "01000-001",
                Geolocation = new AddressGeolocation
                {
                    Lat = "0",
                    Long = "0"
                }
            }
        };

        var customer = new Customer { Name = "Cliente Async Integracao" };
        var category = new Category { Name = "Categoria Async Integracao" };

        db.Users.Add(user);
        db.Customers.Add(customer);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var branch = new Branch
        {
            Name = "Filial Async Integracao",
            Cnpj = "91000000000002",
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        const int initialStock = 30;
        var product = new Product
        {
            Title = "Produto Async Integracao",
            Description = "Produto usado no teste E2E assincrono de venda.",
            Price = 15m,
            CategoryId = category.Id,
            Image = "https://example.test/async-product.png",
            Inventory = new Inventory
            {
                AvailableQuantity = initialStock,
                MinimumStockAlert = 0
            }
        };

        db.Branches.Add(branch);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        const int quantity = 6;
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
            user,
            customer.Id,
            branch.Id,
            product.Id,
            cart.Id,
            quantity);
    }

    private sealed record SaleScenarioSeed(
        User User,
        int CustomerId,
        int BranchId,
        int ProductId,
        int CartId,
        int Quantity);
}
