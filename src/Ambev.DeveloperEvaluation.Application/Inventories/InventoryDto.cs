namespace Ambev.DeveloperEvaluation.Application.Inventories;

public class InventoryDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    /// <summary>Nome da categoria do produto.</summary>
    public string Category { get; set; } = string.Empty;

    public int AvailableQuantity { get; set; }

    /// <summary>Quantidade mínima configurada para disparo de alerta. 0 = alerta desativado.</summary>
    public int MinimumStockAlert { get; set; }

    /// <summary>Indica se o estoque atual está igual ou abaixo do limiar de alerta.</summary>
    public bool IsBelowAlert { get; set; }
}
