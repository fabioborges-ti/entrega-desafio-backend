using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.UpdateCart;

public class UpdateCartCommand : IRequest<CartDto>
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime Date { get; set; }

    public IReadOnlyList<CartLineInput> Products { get; set; } = Array.Empty<CartLineInput>();
}
