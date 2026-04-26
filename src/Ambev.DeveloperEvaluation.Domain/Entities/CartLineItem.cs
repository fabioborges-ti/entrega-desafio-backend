namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Item de carrinho (productId + quantity) conforme <see href="https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/carts-api.md">carts-api</see>.
/// </summary>
public class CartLineItem
{
    public int Id { get; set; }

    public int CartId { get; set; }

    public Cart? Cart { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; }
}
