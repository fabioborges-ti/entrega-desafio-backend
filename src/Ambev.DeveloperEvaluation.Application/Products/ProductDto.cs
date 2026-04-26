namespace Ambev.DeveloperEvaluation.Application.Products;

public class ProductDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    /// <summary>Nome da categoria (somente leitura na API).</summary>
    public string Category { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public ProductRatingDto Rating { get; set; } = new();
}

public class ProductRatingDto
{
    public decimal Rate { get; set; }

    public int Count { get; set; }
}
