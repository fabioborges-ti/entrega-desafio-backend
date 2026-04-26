using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.GetProduct;

public class GetProductCommand : IRequest<ProductDto>
{
    public int Id { get; set; }
}
