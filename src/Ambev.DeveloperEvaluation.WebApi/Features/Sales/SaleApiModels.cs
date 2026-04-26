namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

public class ExternalIdentityResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class SaleItemResponse
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsCancelled { get; set; }
}

