namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

public interface ISaleEventPublisher
{
    void PublishSaleCreated(SaleCreatedPayload payload);

    void PublishSaleModified(SaleModifiedPayload payload);

    void PublishSaleCancelled(SaleCancelledPayload payload);
}
