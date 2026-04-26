using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

public class SaleEventPublisher : ISaleEventPublisher
{
    private readonly ILogger<SaleEventPublisher> _logger;

    public SaleEventPublisher(ILogger<SaleEventPublisher> logger)
    {
        _logger = logger;
    }

    public void PublishSaleCreated(SaleCreatedPayload payload) =>
        _logger.LogInformation(
            "Domain event {DomainEvent} SaleId={SaleId} SaleNumber={SaleNumber} SaleDate={SaleDate} TotalAmount={TotalAmount} OccurredAtUtc={OccurredAtUtc}",
            "SaleCreated",
            payload.SaleId,
            payload.SaleNumber,
            payload.SaleDate,
            payload.TotalAmount,
            payload.OccurredAtUtc);

    public void PublishSaleModified(SaleModifiedPayload payload) =>
        _logger.LogInformation(
            "Domain event {DomainEvent} SaleId={SaleId} SaleNumber={SaleNumber} TotalAmount={TotalAmount} OccurredAtUtc={OccurredAtUtc}",
            "SaleModified",
            payload.SaleId,
            payload.SaleNumber,
            payload.TotalAmount,
            payload.OccurredAtUtc);

    public void PublishSaleCancelled(SaleCancelledPayload payload) =>
        _logger.LogInformation(
            "Domain event {DomainEvent} SaleId={SaleId} SaleNumber={SaleNumber} OccurredAtUtc={OccurredAtUtc}",
            "SaleCancelled",
            payload.SaleId,
            payload.SaleNumber,
            payload.OccurredAtUtc);
}
