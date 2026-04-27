using Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.Messaging.Sales;

public class InMemorySalesMessageStatusStoreTests
{
    [Fact(DisplayName = "TryGet retorna false para correlation id inexistente")]
    public void TryGet_WhenMissing_ReturnsFalse()
    {
        var sut = new InMemorySalesMessageStatusStore();

        var found = sut.TryGet("missing", out var status);

        found.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact(DisplayName = "MarkQueued cria status inicial com payload")]
    public void MarkQueued_CreatesInitialStatus()
    {
        var sut = new InMemorySalesMessageStatusStore();

        sut.MarkQueued("corr-1", "SaleCreated", "{\"id\":1}");
        var found = sut.TryGet("corr-1", out var status);

        found.Should().BeTrue();
        status.Should().NotBeNull();
        status!.State.Should().Be(SalesMessageProcessingState.Queued);
        status.Attempts.Should().Be(0);
        status.PayloadJson.Should().Be("{\"id\":1}");
        status.CreatedAtUtc.Should().NotBe(default);
        status.UpdatedAtUtc.Should().NotBe(default);
    }

    [Fact(DisplayName = "Transições atualizam estado, tentativas e erros")]
    public void MarkMethods_UpdateTransitions()
    {
        var sut = new InMemorySalesMessageStatusStore();
        var correlationId = "corr-2";

        sut.MarkProcessing(correlationId, "SaleCreated", 1);
        sut.MarkRetrying(correlationId, "SaleCreated", 2, TimeSpan.FromSeconds(2.2), "timeout");
        sut.MarkSucceeded(correlationId, "SaleCreated", 3);
        sut.MarkDeadLettered(correlationId, "SaleCreated", 4, "fatal");

        var found = sut.TryGet(correlationId, out var status);

        found.Should().BeTrue();
        status.Should().NotBeNull();
        status!.EventName.Should().Be("SaleCreated");
        status.State.Should().Be(SalesMessageProcessingState.DeadLettered);
        status.Attempts.Should().Be(4);
        status.LastError.Should().Be("fatal");
        status.NextRetryInSeconds.Should().BeNull();
    }
}
