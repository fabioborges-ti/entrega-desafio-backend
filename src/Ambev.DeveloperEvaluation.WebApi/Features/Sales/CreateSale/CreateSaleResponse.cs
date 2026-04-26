namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;

public class CreateSaleResponse
{
    public int Id { get; set; }

    public string SaleNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public int CartId { get; set; }
}

