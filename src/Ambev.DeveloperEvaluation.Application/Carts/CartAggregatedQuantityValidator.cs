using Ambev.DeveloperEvaluation.Application.Carts.CreateCart;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentValidation;
using FluentValidation.Results;

namespace Ambev.DeveloperEvaluation.Application.Carts;

internal static class CartAggregatedQuantityValidator
{
    /// <summary>
    /// Garante que a soma das quantidades por produto não ultrapassa o limite de venda (ex.: 20),
    /// mesmo quando o payload traz várias entradas com o mesmo ProductId.
    /// </summary>
    public static void EnsurePerProductTotalsWithinSaleLimit(IReadOnlyList<CartLineInput> lines)
    {
        try
        {
            foreach (var g in lines.GroupBy(p => p.ProductId))
                QuantityDiscountPolicy.ValidateQuantity(g.Sum(p => p.Quantity));
        }
        catch (DomainException ex)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("Products", ex.Message)
            });
        }
    }
}

