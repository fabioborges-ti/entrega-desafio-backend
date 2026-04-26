namespace Ambev.DeveloperEvaluation.Application.Products.ListProducts;

public class ListProductsResult
{
    public IReadOnlyList<ProductDto> Data { get; set; } = Array.Empty<ProductDto>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}
