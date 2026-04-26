namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleResult
{
    public int Id { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int CartId { get; set; }
}

