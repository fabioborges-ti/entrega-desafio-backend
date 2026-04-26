using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;

namespace Ambev.DeveloperEvaluation.Application.Carts;

internal static class CartLineInventoryValidator
{
    public static async Task EnsureAvailableStockAsync(
        IReadOnlyList<CartLineInput> products,
        IInventoryRepository inventoryRepository,
        CancellationToken cancellationToken)
    {
        var totalsByProductId = products
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity));

        foreach (var (productId, totalQuantity) in totalsByProductId)
        {
            var inventory = await inventoryRepository.GetByProductIdAsync(productId, cancellationToken);
            if (inventory == null || inventory.AvailableQuantity < totalQuantity)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(
                        "Products",
                        $"Quantidade indisponível do produto selecionado (Id: {productId}).")
                });
            }
        }
    }
}

