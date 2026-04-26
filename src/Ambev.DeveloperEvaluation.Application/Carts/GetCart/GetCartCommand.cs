using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.GetCart;

public class GetCartCommand : IRequest<CartDto>
{
    public int Id { get; set; }
}
