namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Estoque disponível para venda de um produto (1:1 com <see cref="Product"/>).
/// </summary>
public class Inventory
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int AvailableQuantity { get; set; }

    /// <summary>
    /// Quantidade mínima de alerta de estoque. Quando <see cref="AvailableQuantity"/>
    /// atingir ou ficar abaixo deste valor, um e-mail de alerta é enviado ao administrador.
    /// Valor 0 desativa o alerta para este produto.
    /// </summary>
    public int MinimumStockAlert { get; set; }

    /// <summary>
    /// Retorna true quando o estoque está igual ou abaixo do limiar de alerta configurado.
    /// Retorna false quando <see cref="MinimumStockAlert"/> é 0 (alerta desativado).
    /// </summary>
    public bool IsStockBelowAlert() =>
        MinimumStockAlert > 0 && AvailableQuantity <= MinimumStockAlert;
}



