using Ambev.DeveloperEvaluation.Application.Sales.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class SaleEventPublisherTests
{
    private readonly CapturingLogger<SaleEventPublisher> _logger = new();

    private SaleEventPublisher CreateSut() => new(_logger);

    [Fact(DisplayName = "PublishSaleCreated emite log Information com nome do evento e payload em JSON")]
    public void PublishSaleCreated_LogsInformation()
    {
        var sut = CreateSut();
        var payload = new SaleCreatedPayload(Random.Shared.Next(1, int.MaxValue), "SN-1", DateTime.UtcNow, 99m, DateTime.UtcNow);

        sut.PublishSaleCreated(payload);

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("SaleCreated");
        entry.Message.Should().Contain(payload.SaleNumber);
        entry.Message.Should().Contain(payload.SaleId.ToString());
    }

    [Fact(DisplayName = "PublishSaleModified emite log Information com nome do evento")]
    public void PublishSaleModified_LogsInformation()
    {
        var sut = CreateSut();
        var payload = new SaleModifiedPayload(Random.Shared.Next(1, int.MaxValue), "SN-2", 50m, DateTime.UtcNow);

        sut.PublishSaleModified(payload);

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("SaleModified");
        entry.Message.Should().Contain(payload.SaleNumber);
    }

    [Fact(DisplayName = "PublishSaleCancelled emite log Information com nome do evento")]
    public void PublishSaleCancelled_LogsInformation()
    {
        var sut = CreateSut();
        var payload = new SaleCancelledPayload(Random.Shared.Next(1, int.MaxValue), "SN-3", DateTime.UtcNow);

        sut.PublishSaleCancelled(payload);

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("SaleCancelled");
        entry.Message.Should().Contain(payload.SaleNumber);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}



