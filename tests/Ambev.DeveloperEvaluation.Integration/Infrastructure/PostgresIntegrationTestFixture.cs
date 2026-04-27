using Ambev.DeveloperEvaluation.ORM;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Infrastructure;

public sealed class PostgresIntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("developer_evaluation_tests")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    public DefaultContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DefaultContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                builder => builder.MigrationsAssembly("Ambev.DeveloperEvaluation.ORM"))
            .Options;

        return new DefaultContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                "SaleItems",
                "Sales",
                "SalesMessageStatuses",
                "CartLineItems",
                "Carts",
                "ProductRatings",
                "Inventories",
                "Products",
                "Categories",
                "Branches",
                "Customers",
                "Users"
            RESTART IDENTITY CASCADE;
            """);
    }
}
