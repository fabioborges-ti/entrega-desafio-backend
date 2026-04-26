using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.CreateCart;

public class CreateCartCommand : IRequest<CartDto>
{
    public int UserId { get; set; }

    public DateTime Date { get; set; }

    public IReadOnlyList<CartLineInput> Products { get; set; } = Array.Empty<CartLineInput>();
}

public class CartLineInput
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}
