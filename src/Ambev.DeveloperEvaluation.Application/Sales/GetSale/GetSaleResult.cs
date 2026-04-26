namespace Ambev.DeveloperEvaluation.Application.Sales.GetSale;

public class GetSaleResult
{
    public int Id { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public ExternalIdentityResult Customer { get; set; } = null!;

    public ExternalIdentityResult Branch { get; set; } = null!;

    public int? CartId { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsCancelled { get; set; }

    public IReadOnlyList<SaleItemResult> Items { get; set; } = Array.Empty<SaleItemResult>();
}

public class ExternalIdentityResult
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class SaleItemResult
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

