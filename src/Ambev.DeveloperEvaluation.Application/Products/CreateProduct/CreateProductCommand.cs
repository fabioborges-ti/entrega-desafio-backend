using Ambev.DeveloperEvaluation.Application.Products;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.CreateProduct;

public class CreateProductCommand : IRequest<ProductDto>
{
    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Image { get; set; } = string.Empty;
}
