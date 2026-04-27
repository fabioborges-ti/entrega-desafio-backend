using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.Common.Logging;
using Ambev.DeveloperEvaluation.WebApi.Configuration;
using Ambev.DeveloperEvaluation.WebApi.Hosting;
using Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Infrastructure;

public sealed class AsyncSalesApiTestFixture : IAsyncLifetime
{
    public const string JwtSecretKey = "integration-tests-secret-key-with-at-least-32-chars";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("developer_evaluation_async_tests")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3.13-alpine")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithCleanUp(true)
        .Build();

    private WebApplication? _app;

    public HttpClient CreateClient() =>
        App.GetTestClient();

    public WebApplication App =>
        _app ?? throw new InvalidOperationException("A WebApi de teste ainda não foi inicializada.");

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DISABLE_HTTPS_REDIRECTION", "true");

        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(ServiceRegistrationExtensions).Assembly.GetName().Name
        });

        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
            ["Jwt:SecretKey"] = JwtSecretKey,
            ["ApiSecrets:Username"] = "integration.admin",
            ["ApiSecrets:Password"] = "Admin@123456",
            ["RabbitMq:HostName"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
            ["RabbitMq:UserName"] = "test_user",
            ["RabbitMq:Password"] = "test_password",
            ["RabbitMq:VirtualHost"] = "/",
            ["StockAlert:CheckIntervalMinutes"] = "60"
        });

        builder.AddDefaultLogging();
        builder.AddWebApiServiceRegistrations();
        KeepOnlySalesConsumerHostedService(builder.Services);

        _app = builder.Build();
        await _app.RunStartupTasksAsync();
        _app.UseWebApiPipeline();
        _app.MapWebApiEndpoints();
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        await _rabbitMq.DisposeAsync();
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

    private static void KeepOnlySalesConsumerHostedService(IServiceCollection services)
    {
        var descriptorsToRemove = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType != typeof(SaleCommandConsumerBackgroundService))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
            services.Remove(descriptor);
    }
}
