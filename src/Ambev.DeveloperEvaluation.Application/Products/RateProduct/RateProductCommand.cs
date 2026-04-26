using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Products.RateProduct;

public class RateProductCommand : IRequest<ProductRatingDto>
{
    public int ProductId { get; set; }

    public int UserId { get; set; }

    public decimal Rate { get; set; }
}
