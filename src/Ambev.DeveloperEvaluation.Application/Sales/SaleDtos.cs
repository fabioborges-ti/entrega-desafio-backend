namespace Ambev.DeveloperEvaluation.Application.Sales;

public class ExternalIdentityDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class SaleItemCommandDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}

