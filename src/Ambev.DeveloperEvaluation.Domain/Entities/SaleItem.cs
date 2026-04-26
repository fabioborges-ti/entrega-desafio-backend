using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsCancelled { get; set; }

    public void RecalculatePricing()
    {
        QuantityDiscountPolicy.ValidateQuantity(Quantity);
        var subtotal = Quantity * UnitPrice;
        DiscountPercent = QuantityDiscountPolicy.GetDiscountRate(Quantity);
        DiscountAmount = Math.Round(subtotal * DiscountPercent, 2, MidpointRounding.AwayFromZero);
        LineTotal = Math.Round(subtotal - DiscountAmount, 2, MidpointRounding.AwayFromZero);
    }
}

