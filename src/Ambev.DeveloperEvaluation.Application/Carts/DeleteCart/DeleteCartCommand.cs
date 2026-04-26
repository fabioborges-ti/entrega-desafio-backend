using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Carts.DeleteCart;

public class DeleteCartCommand : IRequest<DeleteCartResult>
{
    public int Id { get; set; }
}

public class DeleteCartResult
{
    public string Message { get; set; } = string.Empty;
}
