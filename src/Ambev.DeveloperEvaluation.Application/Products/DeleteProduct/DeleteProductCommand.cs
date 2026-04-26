using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.DeleteProduct;

public class DeleteProductCommand : IRequest<DeleteProductResult>
{
    public int Id { get; set; }
}

public class DeleteProductResult
{
    public string Message { get; set; } = string.Empty;
}
