namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Carrinho conforme <see href="https://github.com/coodesh/mouts-backend-challenge/blob/main/.doc/carts-api.md">carts-api</see>.
/// </summary>
public class Cart
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public DateTime Date { get; set; }

    /// <summary>Venda originada deste carrinho (no máximo uma), quando houver checkout.</summary>
    public Sale? Sale { get; set; }

    public ICollection<CartLineItem> LineItems { get; set; } = new List<CartLineItem>();
}

