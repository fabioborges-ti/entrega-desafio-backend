using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales;

/// <summary>
/// Mapeia linhas do carrinho para DTOs de item de venda (preço vem do catálogo no handler).
/// Quantidades do mesmo <see cref="CartLineItem.ProductId"/> são somadas para desconto e limite por produto na venda.
/// </summary>
public static class CartSaleCommandItemMapper
{
    public static IReadOnlyList<SaleItemCommandDto> FromCart(Cart cart)
    {
        var lines = cart.LineItems ?? Array.Empty<CartLineItem>();
        return lines
            .GroupBy(l => l.ProductId)
            .Select(g => new SaleItemCommandDto { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .OrderBy(d => d.ProductId)
            .ToList();
    }
}

