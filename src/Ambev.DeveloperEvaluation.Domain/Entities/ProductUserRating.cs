namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Uma linha de avaliação (usuário + produto + nota). O mesmo par usuário/produto pode ter várias linhas;
/// a média na API usa todas as linhas do produto.
/// </summary>
public class ProductUserRating
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    /// <summary>Nota de 1 a 5.</summary>
    public decimal Rate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

