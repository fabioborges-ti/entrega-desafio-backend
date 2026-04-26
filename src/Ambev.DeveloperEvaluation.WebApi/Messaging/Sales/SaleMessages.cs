namespace Ambev.DeveloperEvaluation.WebApi.Messaging.Sales;

public sealed record CreateSaleRequestedMessage(
    DateTime SaleDate,
    string? SaleNumber,
    int CustomerId,
    int BranchId,
    int CartId);

public sealed record UpdateSaleRequestedMessage(
    int Id,
    DateTime SaleDate,
    int CustomerId,
    int BranchId,
    int CartId);

public sealed record CancelSaleRequestedMessage(
    int Id);

public sealed record DeleteSaleRequestedMessage(
    int Id);

