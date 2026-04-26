using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int BranchId { get; set; }

    public Branch? Branch { get; set; }

    /// <summary>Carrinho de origem da venda (1:1). Nulo para vendas legadas sem carrinho.</summary>
    public int? CartId { get; set; }

    public Cart? Cart { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsCancelled { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();

    public static Sale Create(
        DateTime saleDate,
        int customerId,
        int branchId,
        int cartId,
        IEnumerable<SaleItem> items,
        string? saleNumber = null)
    {
        var sale = new Sale
        {
            SaleNumber = saleNumber ?? SaleNumberGenerator.Generate(),
            SaleDate = saleDate,
            CustomerId = customerId,
            BranchId = branchId,
            CartId = cartId,
            IsCancelled = false
        };

        foreach (var item in items)
        {
            item.SaleId = sale.Id;
            item.RecalculatePricing();
            sale.Items.Add(item);
        }

        sale.RefreshTotal();
        return sale;
    }

    public void EnsureNotCancelled()
    {
        if (IsCancelled)
            throw new DomainException("Operação não permitida: a venda está cancelada.");
    }

    public void ReplaceItems(IEnumerable<SaleItem> newItems)
    {
        EnsureNotCancelled();
        Items.Clear();
        foreach (var item in newItems)
        {
            item.SaleId = Id;
            item.IsCancelled = false;
            item.RecalculatePricing();
            Items.Add(item);
        }

        RefreshTotal();
    }

    public void UpdateHeader(DateTime saleDate, int customerId, int branchId)
    {
        EnsureNotCancelled();
        SaleDate = saleDate;
        CustomerId = customerId;
        BranchId = branchId;
    }

    /// <summary>
    /// Aponta a venda para outro carrinho de origem. A política de estorno/remoção do
    /// cart anterior e a recomposição dos itens é responsabilidade do orquestrador
    /// (handler/repository), pois envolve estoque e persistência.
    /// </summary>
    public void ChangeCart(int newCartId)
    {
        EnsureNotCancelled();
        if (newCartId <= 0)
            throw new DomainException("CartId deve ser um inteiro maior que zero.");
        CartId = newCartId;
    }

    public void Cancel()
    {
        if (IsCancelled)
            return;
        IsCancelled = true;
        TotalAmount = 0;
    }

    public void RefreshTotal()
    {
        TotalAmount = Math.Round(
            Items.Where(i => !i.IsCancelled).Sum(i => i.LineTotal),
            2,
            MidpointRounding.AwayFromZero);
    }
}




