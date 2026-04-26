namespace Ambev.DeveloperEvaluation.Domain.Services;

public static class QuantityDiscountPolicy
{
    public const int MaxQuantityPerProduct = 20;

    /// <summary>
    /// Retorna a alíquota de desconto (0 a 1) conforme a quantidade do item.
    /// 1�?"3: 0%; 4�?"9: 10%; 10�?"20: 20%.
    /// </summary>
    public static decimal GetDiscountRate(int quantity)
    {
        ValidateQuantity(quantity);
        if (quantity < 4)
            return 0m;
        if (quantity <= 9)
            return 0.10m;
        return 0.20m;
    }

    public static void ValidateQuantity(int quantity)
    {
        if (quantity < 1)
            throw new DomainException("A quantidade deve ser pelo menos 1.");
        if (quantity > MaxQuantityPerProduct)
            throw new DomainException($"Não é permitido vender acima de {MaxQuantityPerProduct} itens idênticos (mesmo produto).");
    }
}

