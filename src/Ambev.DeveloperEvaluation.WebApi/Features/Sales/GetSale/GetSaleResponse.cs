using Ambev.DeveloperEvaluation.WebApi.Features.Sales;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;

public class GetSaleResponse
{
    public int Id { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public ExternalIdentityResponse Customer { get; set; } = null!;

    public ExternalIdentityResponse Branch { get; set; } = null!;

    public int? CartId { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsCancelled { get; set; }

    public IReadOnlyList<SaleItemResponse> Items { get; set; } = Array.Empty<SaleItemResponse>();
}

