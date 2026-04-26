namespace Ambev.DeveloperEvaluation.Application.Carts.ListCarts;

public class ListCartsResult
{
    public IReadOnlyList<CartDto> Data { get; set; } = Array.Empty<CartDto>();

    public int TotalItems { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }
}
