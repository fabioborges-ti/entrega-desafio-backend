using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging.Sales;

public class PostgresSalesMessageStatusStoreTests
{
    private static ServiceProvider BuildProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DefaultContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    [Fact(DisplayName = "TryGet retorna false quando correlation id não existe")]
    public void TryGet_WhenStatusDoesNotExist_ReturnsFalse()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString("N"));
        var store = new PostgresSalesMessageStatusStore(provider.GetRequiredService<IServiceScopeFactory>());

        var found = store.TryGet("corr-missing", out var status);

        found.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact(DisplayName = "MarkQueued persiste payload e estado inicial")]
    public void MarkQueued_PersistsInitialStatus()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString("N"));
        var store = new PostgresSalesMessageStatusStore(provider.GetRequiredService<IServiceScopeFactory>());

        store.MarkQueued("corr-1", "SaleCreated", "{\"id\":1}");
        var found = store.TryGet("corr-1", out var status);

        found.Should().BeTrue();
        status.Should().NotBeNull();
        status!.EventName.Should().Be("SaleCreated");
        status.State.Should().Be(SalesMessageProcessingState.Queued);
        status.Attempts.Should().Be(0);
        status.LastError.Should().BeNull();
        status.NextRetryInSeconds.Should().BeNull();
        status.PayloadJson.Should().Be("{\"id\":1}");
        status.CreatedAtUtc.Should().NotBe(default);
        status.UpdatedAtUtc.Should().NotBe(default);
    }

    [Fact(DisplayName = "Transições de estado atualizam tentativas e erro")]
    public void MarkMethods_UpdateStateTransitions()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString("N"));
        var store = new PostgresSalesMessageStatusStore(provider.GetRequiredService<IServiceScopeFactory>());
        var correlationId = "corr-2";

        store.MarkQueued(correlationId, "SaleCreated", null);
        store.MarkProcessing(correlationId, "SaleCreated", 1);
        store.MarkRetrying(correlationId, "SaleCreated", 2, TimeSpan.FromSeconds(3.2), "timeout");
        store.MarkSucceeded(correlationId, "SaleCreated", 3);
        store.MarkDeadLettered(correlationId, "SaleCreated", 4, "fatal");

        var found = store.TryGet(correlationId, out var status);

        found.Should().BeTrue();
        status.Should().NotBeNull();
        status!.State.Should().Be(SalesMessageProcessingState.DeadLettered);
        status.Attempts.Should().Be(4);
        status.LastError.Should().Be("fatal");
        status.NextRetryInSeconds.Should().BeNull();
    }

    [Fact(DisplayName = "TryGet usa fallback para Queued quando estado persistido é inválido")]
    public void TryGet_WithInvalidState_FallsBackToQueued()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString("N"));
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            db.SalesMessageStatuses.Add(new ORM.Messaging.SalesMessageStatusRecord
            {
                CorrelationId = "corr-invalid",
                EventName = "SaleCreated",
                State = "InvalidState",
                Attempts = 7,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                UpdatedAtUtc = DateTime.UtcNow,
                LastError = "invalid state persisted",
                NextRetryInSeconds = 10,
                PayloadJson = "{\"raw\":true}"
            });
            db.SaveChanges();
        }

        var store = new PostgresSalesMessageStatusStore(provider.GetRequiredService<IServiceScopeFactory>());
        var found = store.TryGet("corr-invalid", out var status);

        found.Should().BeTrue();
        status.Should().NotBeNull();
        status!.State.Should().Be(SalesMessageProcessingState.Queued);
        status.Attempts.Should().Be(7);
        status.LastError.Should().Be("invalid state persisted");
        status.NextRetryInSeconds.Should().Be(10);
        status.PayloadJson.Should().Be("{\"raw\":true}");
    }
}
