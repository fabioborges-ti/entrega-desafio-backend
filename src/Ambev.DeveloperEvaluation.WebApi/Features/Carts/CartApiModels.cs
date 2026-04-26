namespace Ambev.DeveloperEvaluation.WebApi.Features.Carts;

public class CartProductRequest
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}

public class CreateCartRequest
{
    public int UserId { get; set; }

    public string Date { get; set; } = string.Empty;

    public List<CartProductRequest> Products { get; set; } = new();
}

public class UpdateCartRequest
{
    public int UserId { get; set; }

    public string Date { get; set; } = string.Empty;

    public List<CartProductRequest> Products { get; set; } = new();
}
