namespace Ambev.DeveloperEvaluation.Application.Sales.Events;

public record SaleCreatedPayload(
    int SaleId,
    string SaleNumber,
    DateTime SaleDate,
    decimal TotalAmount,
    DateTime OccurredAtUtc);

public record SaleModifiedPayload(
    int SaleId,
    string SaleNumber,
    decimal TotalAmount,
    DateTime OccurredAtUtc);

public record SaleCancelledPayload(
    int SaleId,
    string SaleNumber,
    DateTime OccurredAtUtc);

