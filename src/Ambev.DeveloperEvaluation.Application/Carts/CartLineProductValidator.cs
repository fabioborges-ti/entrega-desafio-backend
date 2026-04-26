using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;

namespace Ambev.DeveloperEvaluation.Application.Carts;

internal static class CartLineProductValidator
{
    public static async Task EnsureProductsExistAsync(
        IReadOnlyList<CartLineInput> products,
        IProductRepository productRepository,
        CancellationToken cancellationToken)
    {
        foreach (var productId in products.Select(p => p.ProductId).Distinct())
        {
            var product = await productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(
                        "Products",
                        $"Produto não encontrado (Id: {productId}).")
                });
            }
        }
    }
}

