namespace Ambev.DeveloperEvaluation.WebApi.Features.Products;

public class CreateProductRequest
{
    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Image { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Image { get; set; } = string.Empty;
}

public class RateProductRequest
{
    public decimal Rate { get; set; }
}
