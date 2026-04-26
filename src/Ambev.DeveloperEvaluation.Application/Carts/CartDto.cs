namespace Ambev.DeveloperEvaluation.Application.Carts;

public class CartDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Data no formato ISO (apenas data), conforme contrato da API.</summary>
    public string Date { get; set; } = string.Empty;

    public IReadOnlyList<CartProductDto> Products { get; set; } = Array.Empty<CartProductDto>();
}

public class CartProductDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}
